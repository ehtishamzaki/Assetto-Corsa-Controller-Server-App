using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls.Primitives;

namespace ACControllerServer.Services
{
    public class EventHouseAPI : IEventHouseAPI
    {
        #region Variables

        private readonly ILogger<EventHouseAPI> _logger;
        private readonly EventHouseConfig _config;

        /// <summary>
        /// this client handler will be used to bypass the self signed certificate errors
        /// </summary>
        private readonly HttpClientHandler _RequestHandler;

        /// <summary>
        /// this client will be used to make http requests to server
        /// </summary>
        private readonly HttpClient _Client;

        #endregion

        public EventHouseAPI(ILogger<EventHouseAPI> logger,
            IOptions<EventHouseConfig> eventHouseConfig)
        {
            _logger = logger;
            _config = eventHouseConfig.Value;

            // setup httpclient
            if (string.IsNullOrEmpty(_config.ServerAddress))
                throw new ArgumentNullException(nameof(_config.ServerAddress));
            if (!_config.ServerAddress.EndsWith('/'))
                _config.ServerAddress += "/";
            _RequestHandler = new()
            {
                UseCookies = true,
                UseProxy = false,
                UseDefaultCredentials = false,
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
            _Client = new(_RequestHandler, false) { BaseAddress = new Uri(_config.ServerAddress) };
            _Client.DefaultRequestHeaders.Clear();
            _Client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0");
            _Client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            _Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _config.EventToken);

            _logger.LogDebug("{this} has been initialized", this);
        }

        #region IEventHouseAPI

        public async Task<EventHouseVisitorResponse> GetVisitor(string qrCodeData)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["activation_id"] = _config.ActivationId;
            queryParams["qrcode"] = qrCodeData;
            _logger.LogDebug("making request to /api/rest/search with '{qrCode}' data", qrCodeData);
            // create request to home page
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/rest/search?{queryParams}", UriKind.Relative));
            // get response from server manager
            using var response = await _Client.SendAsync(request);
            // get the string response
            string responseRaw = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("response received from /api/rest/search, Response: '{resp}'", responseRaw);
            return JsonConvert.DeserializeObject<EventHouseVisitorResponse>(responseRaw ?? string.Empty);
        }

        public async Task<bool> PostVisitorResults(
            string visitor_id, 
            string bestlapTimeInMs, 
            string trackId, 
            string trackName, 
            string carId, 
            string carName)
        {
            var reqData = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("access_token", _config.ActivationToken),
                new KeyValuePair<string, string>("visitor_id", visitor_id),
                new KeyValuePair<string, string>("time", bestlapTimeInMs),
                new KeyValuePair<string, string>("track_id", trackId),
                new KeyValuePair<string, string>("track_name", trackName),
                new KeyValuePair<string, string>("car_id", carId),
                new KeyValuePair<string, string>("car_name", carName)
            };
            _logger.LogDebug("making request to /api/kiosk/save_esim_result with '{@reqData}' data", reqData);
            // create request to home page
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/kiosk/save_esim_result", UriKind.Relative));
            request.Content = new FormUrlEncodedContent(reqData);
            // get response from server manager
            using var response = await _Client.SendAsync(request);
            // get the string response
            string responseRaw = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("response received from /api/kiosk/save_esim_result, Response: '{resp}'", responseRaw);
            return response.IsSuccessStatusCode;
        }

        #endregion

    }
}
