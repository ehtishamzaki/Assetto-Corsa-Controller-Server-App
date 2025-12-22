using ACController.Models;
using ACController.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Steamworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ACController
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Point _DriveButtonLocation = new(50d, 150d);
        private readonly string _SteamId;
        private readonly string _ServerBaseAddress;
        private readonly HubConnection _Hub;
        private readonly AppSettings _Configurtion;
        private readonly AcSimulatorService _SimulatorService;
        private readonly AcConfigService _AcConfigService;
        private readonly OscService _OscService;

        private enum SimulatorState { None, Available, Attended, Racing }
        private SimulatorState _currentState = SimulatorState.None;

        public MainWindow()
        {
            AppSettings.LoadConfig(ref _Configurtion);
            if (Environment.GetEnvironmentVariable("SteamAppId") == null)
                Environment.SetEnvironmentVariable("SteamAppId", "244210", EnvironmentVariableTarget.Process);

            if (SteamAPI.Init())
            {
                _SteamId = SteamUser.GetSteamID().ToString();
                SteamAPI.Shutdown();
            }
            else
            {
                _SteamId = Environment.MachineName;
            }

            _ServerBaseAddress = _Configurtion.ServerAddress.Trim('/').Trim();
            _SimulatorService = new AcSimulatorService(_Configurtion.ACRootDirectory);
            _AcConfigService = new AcConfigService();
            _OscService = new OscService();

            InitializeComponent();
#if DEBUG
            this.Topmost = false;
            IsConnected = false;
            NotifyProperty(nameof(IsConnected));
            IsAssigned = false;
            NotifyProperty(nameof(IsAssigned));
            VisitorName = "Husnain Ali";
            NotifyProperty(nameof(VisitorName));
            RacePosition = AddOrdinal(2);
            NotifyProperty(nameof(RacePosition));
            TrackName = "Imola";
            NotifyProperty(nameof(TrackName));
            CarName = "Abarth 500 EsseEsse";
            NotifyProperty(nameof(CarName));
            BestLapTime = TimeSpan.FromMilliseconds(147964).ToString("%m' min. '%s' sec.'");
            NotifyProperty(nameof(BestLapTime));
            IsResultAvailable = false;
            NotifyProperty(nameof(IsResultAvailable));
#endif

            _Hub = new HubConnectionBuilder()
                .WithUrl($"{_ServerBaseAddress}/api/signalr?name={UrlEncoder.Default.Encode(_Configurtion.MachineName)}&steamId={_SteamId}")
                .WithAutomaticReconnect(new SignalRAutoReconnect(5))
                .Build();
            _Hub.Closed += Hub_Closed;
            _ = StartHub();

            _Hub.On("Start", OnStart);
            _Hub.On("Stop", OnStop);
            _Hub.On("SkipGameLobby", OnSkipGameLobby);
            _Hub.On("SimulatorStatus", OnSimulatorStatus);
            _Hub.On<string>("SetDriver", OnSetDriver);
            _Hub.On<string, string>("SetCurrentCar", OnSetCurrentCar);
            _Hub.On<ACRemoteConfig>("SetRemoteServer", OnSetRemoteServer);
            _Hub.On<string, string>("SetCurrentTrack", OnSetCurrentTrack);
            _Hub.On<int, string, string, long>("RaceResult", OnRaceResult);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MouseHook.LeftMouseClick(_DriveButtonLocation);
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyProperty(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region SignalR

        private async Task StartHub()
        {
            await _Hub.StartAsync().ContinueWith(async task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    IsConnected = true;
                    NotifyProperty(nameof(IsConnected));
                    Dispatcher.Invoke(UpdateState);
                    return;
                }
                if (task.IsFaulted || task.IsCanceled)
                {
                    if (task.IsFaulted)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show(this, task.Exception!.Message +
                                "\n\nPlease verify the connection with server and try again\n" +
                                "Application will now exit!",
                                "Assetto Corsa Controller", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    Dispatcher.Invoke(Application.Current.Shutdown);
                    return;
                }
            });
        }

        private Task Hub_Closed(Exception arg)
        {
            Dispatcher.Invoke(() => this.OnSetDriver(string.Empty));
            return Task.CompletedTask;
        }

        private void OnStart()
        {
            if (IsAssigned)
            {
                _SimulatorService.StartSimulator();
                UpdateState();
            }
        }

        private void OnStop()
        {
            _SimulatorService.StopSimulator();
            UpdateState();
        }

        private void OnRaceResult(int position,
            string trackName,
            string carName,
            long bestLapMilliseconds)
        {
            if (!IsAssigned)
                return;

            Dispatcher.Invoke(() =>
            {
                RacePosition = AddOrdinal(position);
                NotifyProperty(nameof(RacePosition));
                TrackName = trackName;
                NotifyProperty(nameof(TrackName));
                CarName = carName;
                NotifyProperty(nameof(CarName));
                BestLapTime = TimeSpan.FromMilliseconds(bestLapMilliseconds)
                    .ToString("%m' min. '%s' sec.'");
                NotifyProperty(nameof(BestLapTime));
                IsResultAvailable = true;
                NotifyProperty(nameof(IsResultAvailable));
                this.Show();
            });

            _SimulatorService.StopSimulator();
            UpdateState();

            Dispatcher.Invoke(async () =>
            {
                await Task.Delay(1000 * 10);
                await _Hub.StopAsync();
                await StartHub();
            });
        }

        private async void OnSimulatorStatus()
        {
            try
            {
                await _SimulatorService.FindProcess();
                await _Hub.InvokeAsync("UpdateStatus", _SimulatorService.IsRunning);
                Dispatcher.Invoke(UpdateState);
            }
            catch
            {
            }

            if (_SimulatorService.IsRunning)
                Dispatcher.Invoke(() => this.Hide());
            else if (!_SimulatorService.IsRunning && this.Visibility == Visibility.Hidden)
            {
                if (IsResultAvailable)
                    return;
                await _Hub.StopAsync();
                Dispatcher.Invoke(() => this.Show());
                await StartHub();
            }
        }

        private void OnSetRemoteServer(ACRemoteConfig remoteConfig)
        {
            if (remoteConfig != null)
                if (string.IsNullOrWhiteSpace(remoteConfig.ip))
                {
                    remoteConfig.ip = new Uri(_ServerBaseAddress).GetComponents(UriComponents.Host, UriFormat.SafeUnescaped);
                    if (IPAddress.TryParse(remoteConfig.ip, out IPAddress address))
                        remoteConfig.ip = address.MapToIPv4().ToString();
                    else
                        remoteConfig.ip = Dns.GetHostAddresses(remoteConfig.ip)
                            .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)
                            .ToString();
                }

            _AcConfigService.SetRemoteServer(remoteConfig);
        }

        private void OnSkipGameLobby()
        {
            Thread.Sleep(2500);
            MouseHook.LeftMouseClick(_DriveButtonLocation);
        }

        private void OnSetDriver(string driverName)
        {
            if (string.IsNullOrWhiteSpace(driverName))
            {
                driverName = string.Empty;
                VisitorName = driverName;
                NotifyProperty(nameof(VisitorName));
                IsAssigned = false;
                NotifyProperty(nameof(IsAssigned));
                IsResultAvailable = false;
                NotifyProperty(nameof(IsResultAvailable));
                UpdateState();
                return;
            }

            _AcConfigService.SetDriverName(driverName);
            VisitorName = driverName;
            NotifyProperty(nameof(VisitorName));
            IsAssigned = true;
            NotifyProperty(nameof(IsAssigned));
            UpdateState();
        }

        private void OnSetCurrentCar(string carId, string skinId)
        {
            _AcConfigService.SetCarDetails(carId, skinId);
        }

        private void OnSetCurrentTrack(string carId, string skinId)
        {
            _AcConfigService.SetTrackDetails(carId, skinId);
        }

        #endregion

        #region Properties

        public bool IsConnected { get; set; } = false;
        public bool IsAssigned { get; set; } = false;
        public string VisitorName { get; set; } = string.Empty;
        public bool IsResultAvailable { get; set; } = false;
        public string RacePosition { get; set; }
        public string TrackName { get; set; }
        public string CarName { get; set; }
        public string BestLapTime { get; set; }

        #endregion

        #region Methods

        private void UpdateState()
        {
            SimulatorState newState;

            if (!IsConnected)
                newState = SimulatorState.None;
            else if (_SimulatorService.IsRunning)
                newState = SimulatorState.Racing;
            else if (IsAssigned)
                newState = SimulatorState.Attended;
            else
                newState = SimulatorState.Available;

            if (newState != _currentState && newState != SimulatorState.None)
            {
                _currentState = newState;
                int scene = newState switch
                {
                    SimulatorState.Available => 2,
                    SimulatorState.Attended => 3,
                    SimulatorState.Racing => 4,
                    _ => 2
                };
                _OscService.SendScene(scene);
            }
        }

        public static string AddOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            return (num % 10) switch
            {
                1 => num + "st",
                2 => num + "nd",
                3 => num + "rd",
                _ => num + "th"
            };
        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var buttonSize = PresentationSource.FromVisual(btnCoordinates).CompositionTarget
                .TransformToDevice.Transform(new Point(btnCoordinates.Width, btnCoordinates.Height));
            var buttonCoordinates = btnCoordinates.PointToScreen(new Point(0, 0));
            var clickPoint = new Point((buttonSize.X / 2d) + buttonCoordinates.X,
                (buttonSize.Y / 2d) + buttonCoordinates.Y);
            MessageBox.Show(clickPoint.ToString());
        }
    }
}