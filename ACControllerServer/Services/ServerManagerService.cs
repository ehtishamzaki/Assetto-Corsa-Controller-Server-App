using ACControllerServer.SignalR;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ACControllerServer.Services
{
    public class ServerManagerService : IServerManagerService, IDisposable
    {
        public ServerManagerService(ILogger<ServerManagerService> logger,
            IOptions<ServerManagerConfig> serverManagerConfig,
            IACClientService clientService,
            IEventHouseAPI eventHouseAPI)
        {
            _logger = logger;
            _clientService = clientService;
            _eventHouseAPI = eventHouseAPI;
            _config = serverManagerConfig.Value;
            _cookieContainer = new CookieContainer();
            _websocketClient = new WebsocketClient((int)TimeSpan.FromMinutes(5).TotalMilliseconds, AutoReconnet: true);
            _websocketClient.OnResponseReceived_CallBack = OnWebsocketReceive;

            // setup httpclient
            if (string.IsNullOrEmpty(_config.ServerAddress))
                throw new ArgumentNullException(nameof(_config.ServerAddress));
            if (!_config.ServerAddress.EndsWith('/'))
                _config.ServerAddress += "/";
            _RequestHandler = new()
            {
                UseCookies = true,
                CookieContainer = _cookieContainer,
                UseProxy = false,
                UseDefaultCredentials = false,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _Client = new(_RequestHandler, false) { BaseAddress = new Uri(_config.ServerAddress) };
            _Client.DefaultRequestHeaders.Clear();
            _Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0");
            _Client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            _Client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");

            _loginCheckInterval = new Timer(OnLoginCheck_Callback, null, 
                1000, Timeout.Infinite);
            
            _logger.LogInformation("{this} has been initialized", this);
        }

        #region Variables

        private const int _LoginCheckInterval = 5000;
        private static object race_lock = new object();

        private static Dictionary<string, DriverData> _RaceCompletedGUIDs = new Dictionary<string, DriverData>();
        private readonly IEventHouseAPI _eventHouseAPI;
        private readonly IACClientService _clientService;
        private readonly ILogger<ServerManagerService> _logger;
        private readonly ServerManagerConfig _config;
        private readonly CookieContainer _cookieContainer;
        private readonly WebsocketClient _websocketClient;
        private readonly Timer _loginCheckInterval;

        /// <summary>
        /// this client handler will be used to bypass the self signed certificate errors
        /// </summary>
        private readonly HttpClientHandler _RequestHandler;

        /// <summary>
        /// this client will be used to make http requests to server
        /// </summary>
        private readonly HttpClient _Client;

        #endregion

        #region Methods

        private async void OnLoginCheck_Callback(object x)
        {
            try
            {
                await CheckLoggedIn();
            }
            finally
            {
                _loginCheckInterval.Change(_LoginCheckInterval, Timeout.Infinite);
            }
        }

        #endregion

        #region Server Manager

        private async Task LoginServerManager(string username, string password)
        {
            IsLoggedIn = false;
            LoginStatusChanged?.Invoke(null, null);
            // make the first request for collecting session cookies
            {
                // create request to home page
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/login", UriKind.Relative));
                // get response from server manager
                using var response = await _Client.SendAsync(request);
            }

            // create request for checking tags
            using var loginRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("/login", UriKind.Relative));
            loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "Username", username },
                { "Password", password }
            });
            // get response from server manager
            using var loginResponse = await _Client.SendAsync(loginRequest); 
            // check if login successful
            string loginResponseRaw = await loginResponse.Content.ReadAsStringAsync();

            if (loginResponseRaw.Contains("Thanks for logging in!"))
            {
                IsLoggedIn = true;
                _websocketClient.Cookies = _cookieContainer;
                await _websocketClient.Connect($"ws://{_Client.BaseAddress.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped)}/api/race-control")
                    .ConfigureAwait(false);
            }
            LoginStatusChanged?.Invoke(null, null);
        }

        private async Task LogoutServerManager()
        {
            // create request to home page
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/login", UriKind.Relative));
            // get response from server manager
            using var response = await _Client.SendAsync(request);

            IsLoggedIn = false;
            LoginStatusChanged?.Invoke(null, null);
        }

        private async Task CheckLoggedIn()
        {
            string rawResponse = string.Empty;
            try
            {
                // create request to home page
                using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
                // get response from server manager
                using var response = await _Client.SendAsync(request, 
                    HttpCompletionOption.ResponseHeadersRead, 
                    new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
                // get raw response from server manager
                rawResponse = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                IsLoggedIn = false;
                LoginStatusChanged?.Invoke(null, null);
                return;
            }
            catch (TaskCanceledException)
            {
                IsLoggedIn = false;
                LoginStatusChanged?.Invoke(null, null);
                return;
            }
            
            if (rawResponse.Contains("You do not have permission to access this Server Manager instance."))
                await LoginServerManager(_config.LoginUsername, _config.LoginPassword);
            else if (rawResponse.Contains("You must log in to access Server Manager features."))
                await LoginServerManager(_config.LoginUsername, _config.LoginPassword);
            else if (!IsLoggedIn)
            {
                IsLoggedIn = true;
                LoginStatusChanged?.Invoke(null, null);
            }
        }
        
        private async void OnWebsocketReceive(string message)
        {
            await Task.Yield();

            // get the event type
            var responseEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerManagerWebsocketEventtype>(message);
            if (responseEvent is null || responseEvent.EventType == default)
                return;

            // check if event type supported
            switch (responseEvent.EventType)
            {
                case 200:
                    try
                    {
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<ServerManagerWebsocketResponse>(message);
                        if (data is null || data.Message is null)
                            break;

                        _logger.LogInformation("{$Response}", message);
                        
                        // check if race not started
                        if (data.Message.SessionInfo.ElapsedMilliseconds < TimeSpan.FromSeconds(data.Message.SessionInfo.WaitTime).TotalMilliseconds)
                            break;
                        
                        // loop through each driver in the connected drivers
                        foreach (var driver in data.Message.ConnectedDrivers?.Drivers)
                        {
                            if (!driver.Value.HasCompletedSession)
                                continue;

                            lock (race_lock)
                            {
                                if (!_RaceCompletedGUIDs.ContainsKey(driver.Key))
                                    _RaceCompletedGUIDs.Add(driver.Key, driver.Value);
                                else
                                    continue;
                            }

                            var acClient = _clientService.ConnectedClients.FirstOrDefault(x => x.Id == driver.Key);
                            var clientHub = _clientService.Clients.User(driver.Key);

                            if (acClient is null || clientHub is null)
                                lock (race_lock)
                                    if (_RaceCompletedGUIDs.ContainsKey(driver.Key))
                                        _RaceCompletedGUIDs.Remove(driver.Key);

                            long bestLapTime = driver.Value.Cars.ElementAt(0).Value.BestLap / 1_000_000L;
                            await clientHub.SendCoreAsync("RaceResult", new object[] {
                                data.Message.DisconnectedDrivers.Drivers.Count + driver.Value.Position,
                                data.Message.TrackInfo.name,
                                driver.Value.CarInfo.CarName,
                                bestLapTime
                            });
                            await _eventHouseAPI.PostVisitorResults(acClient.AssignedId, 
                                bestLapTime.ToString(), 
                                data.Message.SessionInfo.Track,
                                data.Message.TrackInfo.name,
                                driver.Value.CarInfo.CarModel,
                                driver.Value.CarInfo.CarName).ConfigureAwait(false);
                            
                        }

                        // check if no connected drivers
                        if (data.Message.ConnectedDrivers?.Drivers?.Count < 1)
                        {
                            OnRaceEnd?.Invoke(null, null);
                            _RaceCompletedGUIDs.Clear();
                            break;
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    break;

                case 101:
                    try
                    {
                        var connectedDriver = Newtonsoft.Json.JsonConvert.DeserializeObject<DriverConnectedWebsocketResponse>(message);
                        if (connectedDriver is null || connectedDriver.Message is null)
                            break;
                        _logger.LogInformation("{@Response}", connectedDriver.Message);

                        // get the signalR hub for driver
                        var clientHub = _clientService.Clients.User(connectedDriver.Message.DriverGUID);
                        if (clientHub is not null)
                            // send the signal to simulator for skipping the lobby
                            await clientHub.SendCoreAsync("SkipGameLobby", new object[] { });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }
                    break;

                //case 53:
                //    try
                //    {
                //        var driverState = Newtonsoft.Json.JsonConvert.DeserializeObject<DriverStateWebsocketResponse>(message);
                //        if (driverState is null || driverState.Message is null)
                //            break;

                //        _logger.LogInformation("{@Response}", driverState.Message);

                //        string driverGuid = string.Empty;
                //        if (!CarIdToGUID.ContainsKey(driverState.Message.CarID))
                //            break;
                //        driverGuid = CarIdToGUID[driverState.Message.CarID];

                //        // check if session completed
                //        if (driverState.Message.HasCompletedSession)
                //        {
                //            var clientHub = _clientService.Clients.User(driverGuid);
                //            await clientHub.SendCoreAsync("RaceResult", new object[] {
                //                driverState.Message.RacePosition,
                //                data.Message.TrackInfo.name,
                //                driver.Value.CarInfo.CarName,
                //                driver.Value.Cars.ElementAt(0).Value.BestLap / 1_000_000L
                //            });
                //        }

                //    }
                //    catch (Exception ex)
                //    {
                //        _logger.LogError(ex, ex.Message);
                //    }
                //    break;

                default:
                    _logger.LogDebug("{$Response}", message);
                    break;
            }

            
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            LogoutServerManager().ContinueWith(task =>
            {
                _Client.Dispose();
                _RequestHandler.Dispose();
            }).ConfigureAwait(false);
        }

        #endregion

        #region IServerManagerService

        public event EventHandler LoginStatusChanged;

        public event EventHandler OnRaceStart;
        public event EventHandler OnRaceEnd;

        public bool IsLoggedIn { get; private set; }

        public async Task<bool> HostQuickRace(string trackId, string layoutId, string[] carIds,
            string qualifyTime = "10", string raceTime = "10", string raceLaps = "0")
        {
            await CheckLoggedIn();
            if (!IsLoggedIn)
                return false;

            // create request for checking tags
            using var reqQuickRace = new HttpRequestMessage(HttpMethod.Post, new Uri("/quick/submit", UriKind.Relative));
            var reqData = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("Track", trackId),
                new KeyValuePair<string, string>("TrackLayout", layoutId == "ui" ? "<default>" : layoutId),
                new KeyValuePair<string, string>("q", string.Empty),
                new KeyValuePair<string, string>("Qualifying.Time", qualifyTime),
                new KeyValuePair<string, string>("Race.Time", raceTime),
                new KeyValuePair<string, string>("Race.Laps", raceLaps)
            };
            // add all the selected cars
            Array.ForEach(carIds, carId => reqData.Add(new KeyValuePair<string, string>("Cars", carId)));
            reqQuickRace.Content = new FormUrlEncodedContent(reqData.AsEnumerable());
            // get response from server manager
            using var resQuickRace = await _Client.SendAsync(reqQuickRace);
            // check if login successful
            string rawQuickRace = await resQuickRace.Content.ReadAsStringAsync();

            if (rawQuickRace.Contains("Quick race successfully started!"))
                return true;
            else
                return false;
        }

        public async Task<bool> HostSimpleRace(string trackId, string layoutId, string[] carIds,
            string raceTime = "10", string raceLaps = "0", string maxClients = "2")
        {
            await CheckLoggedIn();
            if (!IsLoggedIn)
                return false;

            // create request for checking tags
            using var reqSimpleRace = new HttpRequestMessage(HttpMethod.Post, new Uri("/custom/new/submit", UriKind.Relative));
            var reqData = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("Track", trackId),
                new KeyValuePair<string, string>("TrackLayout", layoutId == "ui" ? "<default>" : layoutId),
                new KeyValuePair<string, string>("q", string.Empty),
                new KeyValuePair<string, string>("TimeAttack", "0"),
                new KeyValuePair<string, string>("DriftModeEnabled", "0"),
                new KeyValuePair<string, string>("DriftModeSendChatMessages", "0"),
                new KeyValuePair<string, string>("Race.Enabled", "1"),
                new KeyValuePair<string, string>("Race.Name", "Race"),
                new KeyValuePair<string, string>("Race.Time", raceTime),
                new KeyValuePair<string, string>("Race.Laps", raceLaps),
                new KeyValuePair<string, string>("Race.IsOpen", "1"),
                new KeyValuePair<string, string>("Race.WaitTime", "60"),
                new KeyValuePair<string, string>("RacePitWindowStart", "0"),
                new KeyValuePair<string, string>("RacePitWindowEnd", "0"),
                new KeyValuePair<string, string>("RaceOverTime", "180"),
                new KeyValuePair<string, string>("RaceGasPenaltyDisabled", "0"),
                new KeyValuePair<string, string>("ReversedGridRacePositions", "0"),
                new KeyValuePair<string, string>("ReversedGridRaceSource", "0"),
                new KeyValuePair<string, string>("RaceExtraLap", "0"),
                new KeyValuePair<string, string>("Race.DisablePushToPass", "0"),
                new KeyValuePair<string, string>("Qualifying.Enabled", "0"),
                new KeyValuePair<string, string>("Qualifying.Name", "Qualify"),
                new KeyValuePair<string, string>("Qualifying.Time", "10"),
                new KeyValuePair<string, string>("Qualifying.QualifyingType", "0"),
                new KeyValuePair<string, string>("Qualifying.QualifyingNumberOfLapsToAverage", "0"),
                new KeyValuePair<string, string>("Qualifying.VisibilityMode", "0"),
                new KeyValuePair<string, string>("Qualifying.CountOutLap", "0"),
                new KeyValuePair<string, string>("Qualifying.IsOpen", "1"),
                new KeyValuePair<string, string>("QualifyMaxWaitPercentage", "200"),
                new KeyValuePair<string, string>("Practice.Enabled", "0"),
                new KeyValuePair<string, string>("Practice.Name", "Practice"),
                new KeyValuePair<string, string>("Practice.Time", "10"),
                new KeyValuePair<string, string>("Practice.CountOutLap", "0"),
                new KeyValuePair<string, string>("Practice.IsOpen", "1"),
                new KeyValuePair<string, string>("Practice.DisablePushToPass", "0"),
                new KeyValuePair<string, string>("Booking.Enabled", "0"),
                new KeyValuePair<string, string>("Booking.Name", "Booking"),
                new KeyValuePair<string, string>("Booking.Time", "15"),
                new KeyValuePair<string, string>("MaxClients", maxClients),
                new KeyValuePair<string, string>("PickupModeEnabled", "0"),
                new KeyValuePair<string, string>("LockedEntryList", "0"),
                new KeyValuePair<string, string>("DriverSwapEnabled", "0"),
                new KeyValuePair<string, string>("DriverSwapMinTime", "120"),
                new KeyValuePair<string, string>("DriverSwapDisqualifyTime", "30"),
                new KeyValuePair<string, string>("DriverSwapPenaltyTime", "0"),
                new KeyValuePair<string, string>("DriverSwapPenaltyType", "4"),
                new KeyValuePair<string, string>("DriverSwapBoPAmount", "50"),
                new KeyValuePair<string, string>("DriverSwapBoPNumLaps", "2"),
                new KeyValuePair<string, string>("DriverSwapDriveThroughNumLaps", "2"),
                new KeyValuePair<string, string>("DriverSwapDriveThroughAddSwapTime", "0"),
                new KeyValuePair<string, string>("DriverSwapTimeDuration", "20"),
                new KeyValuePair<string, string>("DriverSwapMinimumNumberOfSwaps", "0"),
                new KeyValuePair<string, string>("DriverSwapNotEnoughSwapsPenalty", "0"),
                new KeyValuePair<string, string>("NumEntrantsToAdd", "1"),
                new KeyValuePair<string, string>("EntryList.NumEntrants", "0"),
                new KeyValuePair<string, string>("ABSAllowed", "1"),
                new KeyValuePair<string, string>("TractionControlAllowed", "1"),
                new KeyValuePair<string, string>("StabilityControlAllowed", "0"),
                new KeyValuePair<string, string>("AutoClutchAllowed", "0"),
                new KeyValuePair<string, string>("TyreBlanketsAllowed", "1"),
                new KeyValuePair<string, string>("TimeOfDay", "16%3A00"),
                new KeyValuePair<string, string>("SunAngle", "48"),
                new KeyValuePair<string, string>("TimeOfDayMultiplier", "0"),
                new KeyValuePair<string, string>("TimeMulti", "0"),
                new KeyValuePair<string, string>("CSPTransition.Enabled", "0"),
                new KeyValuePair<string, string>("DateUnix", "2023-08-26T11%3A43"),
                new KeyValuePair<string, string>("Graphics", "3_clear"),
                new KeyValuePair<string, string>("Duration", "0"),
                new KeyValuePair<string, string>("CSPTransitionDuration", "240"),
                new KeyValuePair<string, string>("RainPresetID", ""),
                new KeyValuePair<string, string>("BaseTemperatureAmbient", "26"),
                new KeyValuePair<string, string>("BaseTemperatureRoad", "11"),
                new KeyValuePair<string, string>("VariationAmbient", "1"),
                new KeyValuePair<string, string>("VariationRoad", "1"),
                new KeyValuePair<string, string>("WindBaseSpeedMin", "3"),
                new KeyValuePair<string, string>("WindBaseSpeedMax", "15"),
                new KeyValuePair<string, string>("WindBaseDirection", "30"),
                new KeyValuePair<string, string>("WindVariationDirection", "15"),
                new KeyValuePair<string, string>("FuelRate", "100"),
                new KeyValuePair<string, string>("DamageMultiplier", "0"),
                new KeyValuePair<string, string>("TyreWearRate", "100"),
                new KeyValuePair<string, string>("ForceVirtualMirror", "1"),
                new KeyValuePair<string, string>("ForceOpponentHeadlights", "0"),
                new KeyValuePair<string, string>("SurfacePreset", ""),
                new KeyValuePair<string, string>("SessionStart", "100"),
                new KeyValuePair<string, string>("Randomness", "0"),
                new KeyValuePair<string, string>("SessionTransfer", "100"),
                new KeyValuePair<string, string>("LapGain", "10"),
                new KeyValuePair<string, string>("LegalTyres", "H"),
                new KeyValuePair<string, string>("LegalTyres", "M"),
                new KeyValuePair<string, string>("LegalTyres", "S"),
                new KeyValuePair<string, string>("MaxBallastKilograms", "50"),
                new KeyValuePair<string, string>("AllowedTyresOut", "3"),
                new KeyValuePair<string, string>("MaxContactsPerKilometer", "-1"),
                new KeyValuePair<string, string>("AutoKickIdleTime", "0"),
                new KeyValuePair<string, string>("AutoKickBlockListMode", "0"),
                new KeyValuePair<string, string>("StartRule", "2"),
                new KeyValuePair<string, string>("LoopMode", "0"),
                new KeyValuePair<string, string>("ResultScreenTime", "5"),
                new KeyValuePair<string, string>("DisableDRSZones", "0"),
                new KeyValuePair<string, string>("DisableChecksums", "1"),
                new KeyValuePair<string, string>("ChecksumTrackKN5", "0"),
                new KeyValuePair<string, string>("ChecksumCarKN5", "0"),
                new KeyValuePair<string, string>("OnlyChecksumACDOfChosenCar", "0"),
                new KeyValuePair<string, string>("CustomCutsEnabled", "0"),
                new KeyValuePair<string, string>("CustomCutsNumWarnings", "4"),
                new KeyValuePair<string, string>("CustomCutsPenaltyType", "0"),
                new KeyValuePair<string, string>("CustomCutsBoPAmount", "50"),
                new KeyValuePair<string, string>("CustomCutsBoPNumLaps", "1"),
                new KeyValuePair<string, string>("CustomCutsDriveThroughNumLaps", "2"),
                new KeyValuePair<string, string>("CustomCutsTimeDuration", "5"),
                new KeyValuePair<string, string>("CustomCutsOnlyIfCleanSet", "0"),
                new KeyValuePair<string, string>("CustomCutsIgnoreFirstLap", "1"),
                new KeyValuePair<string, string>("CollisionPenaltiesEnabled", "0"),
                new KeyValuePair<string, string>("CollisionPenaltiesTypeToPenalise", "0"),
                new KeyValuePair<string, string>("CollisionPenaltiesNumWarnings", "4"),
                new KeyValuePair<string, string>("CollisionPenaltiesOnlyOverSpeed", "40"),
                new KeyValuePair<string, string>("CollisionPenaltiesPenaltyType", "4"),
                new KeyValuePair<string, string>("CollisionPenaltiesBoPAmount", "50"),
                new KeyValuePair<string, string>("CollisionPenaltiesBoPNumLaps", "2"),
                new KeyValuePair<string, string>("CollisionPenaltiesDriveThroughNumLaps", "2"),
                new KeyValuePair<string, string>("CollisionPenaltiesTimeDuration", "2"),
                new KeyValuePair<string, string>("CollisionPenaltiesIgnoreFirstLap", "1"),
                new KeyValuePair<string, string>("DRSPenaltiesEnabled", "0"),
                new KeyValuePair<string, string>("DRSPenaltiesWindow", "1"),
                new KeyValuePair<string, string>("DRSPenaltiesEnableOnLap", "3"),
                new KeyValuePair<string, string>("DRSPenaltiesNumWarnings", "2"),
                new KeyValuePair<string, string>("DRSPenaltiesPenaltyType", "1"),
                new KeyValuePair<string, string>("DRSPenaltiesBoPAmount", "50"),
                new KeyValuePair<string, string>("DRSPenaltiesBoPNumLaps", "2"),
                new KeyValuePair<string, string>("DRSPenaltiesDriveThroughNumLaps", "2"),
                new KeyValuePair<string, string>("DRSPenaltiesTimeDuration", "5"),
                new KeyValuePair<string, string>("TyrePenaltiesEnabled", "0"),
                new KeyValuePair<string, string>("TyrePenaltiesMinimumCompounds", "2"),
                new KeyValuePair<string, string>("TyrePenaltiesMinimumCompoundsPenalty", "0"),
                new KeyValuePair<string, string>("TyrePenaltiesMustStartOnBestQualifying", "1"),
                new KeyValuePair<string, string>("TyrePenaltiesBestQualifyingForSecondRace", "0"),
                new KeyValuePair<string, string>("TyrePenaltiesPenaltyType", "4"),
                new KeyValuePair<string, string>("TyrePenaltiesBoPAmount", "50"),
                new KeyValuePair<string, string>("TyrePenaltiesBoPNumLaps", "2"),
                new KeyValuePair<string, string>("TyrePenaltiesDriveThroughNumLaps", "2"),
                new KeyValuePair<string, string>("TyrePenaltiesTimeDuration", "10"),
                new KeyValuePair<string, string>("SpeedTrapsSendChatMessages", "0"),
                new KeyValuePair<string, string>("OverridePassword", "0"),
                new KeyValuePair<string, string>("ReplacementPassword", ""),
                new KeyValuePair<string, string>("ForceStopTime", ""),
                new KeyValuePair<string, string>("ForceStopWithDrivers", "0"),
                new KeyValuePair<string, string>("CustomRaceName", "SimpleRace"),
                new KeyValuePair<string, string>("CustomRaceScheduled", ""),
                new KeyValuePair<string, string>("CustomRaceScheduledTime", ""),
                new KeyValuePair<string, string>("CustomRaceScheduledTimezone", ""),
                new KeyValuePair<string, string>("event-schedule-recurrence", ""),
                new KeyValuePair<string, string>("action", "startRace"),
            };

            // add all the selected cars
            Array.ForEach(carIds, carId => reqData.Add(new KeyValuePair<string, string>("Cars", carId)));
            reqSimpleRace.Content = new FormUrlEncodedContent(reqData.AsEnumerable());
            // get response from server manager
            using var resSimpleRace = await _Client.SendAsync(reqSimpleRace);
            // check if login successful
            string rawSimpleRace = await resSimpleRace.Content.ReadAsStringAsync();

            if (rawSimpleRace.Contains("Custom race started!"))
                return true;
            else
                return false;
        }

        public async Task<bool> StopHostedEvent()
        {
            await CheckLoggedIn();
            if (!IsLoggedIn)
                return false;

            // create request for checking tags
            using var reqMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(_Client.BaseAddress, "/process/stop"));
            reqMessage.Headers.Referrer = new Uri(_Client.BaseAddress, "/");
            // get response from server manager
            using var resStopHost = await _Client.SendAsync(reqMessage);
            // check if login successful
            string rawResponse = await resStopHost.Content.ReadAsStringAsync();

            if (rawResponse.Contains("Server successfully stopped"))
                return true;
            else
                return false;
        }

        public async Task<ACRemoteConfig> GetRemoteServerConfig()
        {
            // get the server manage host
            string host = _Client.BaseAddress.GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
            // create request message for getting server info
            using var reqMessage = new HttpRequestMessage(HttpMethod.Get, new Uri($"http://{host}:8081/INFO", UriKind.Absolute));
            // get the response for remote server info
            using var resMessage = await _Client.SendAsync(reqMessage);
            var config = await resMessage.Content.ReadFromJsonAsync<ACRemoteConfig>();
            config.ip = host;
            return config;
        }

        #endregion

    }
}
