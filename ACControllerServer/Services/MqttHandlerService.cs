using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Services
{

    internal class MqttHandlerService : IMqttHandlerService
    {
        public MqttHandlerService(ILogger<MqttHandlerService> logger,
            IOptions<MqttServerConfig> mqttConfig)
        {
            // assign readonly
            _logger = logger;
            _MqttConfig = mqttConfig.Value;

            // setup MQTT
            _mqttFactory = new MqttFactory();
            _mqttClient = _mqttFactory.CreateMqttClient();
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(_MqttConfig.ServerAddress, _MqttConfig.ServerPort)
                .WithCredentials(_MqttConfig.LoginUsername, _MqttConfig.LoginPassword)
                .WithTlsOptions(o => {
                    o.WithCertificateValidationHandler(_ => true);
                    o.WithSslProtocols(SslProtocols.Tls12);
                })
                .Build();

            _mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;
        }

        private async Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            if (arg.ClientWasConnected)
                // Use the current options as the new options.
                await _mqttClient.ConnectAsync(_mqttClientOptions);
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            await arg.AcknowledgeAsync(CancellationToken.None);
            throw new NotImplementedException();
        }

        #region Variables

        private readonly ILogger<MqttHandlerService> _logger;
        private readonly MqttServerConfig _MqttConfig;

        private readonly MqttFactory _mqttFactory;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttClientOptions;

        #endregion

        #region IMqttHandlerService

        #endregion

    }

}
