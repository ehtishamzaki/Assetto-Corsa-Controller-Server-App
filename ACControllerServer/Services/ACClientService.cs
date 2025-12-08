using ACControllerServer.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Services
{
    public class ACClientService : IACClientService
    {
        private readonly ILogger<ACClientService> _logger;
        private readonly IHubContext<ClientHub> _Context;

        public ACClientService(ILogger<ACClientService> logger, IHubContext<ClientHub> context)
        {
            _logger = logger;
            _Context = context;
        }

        #region IACClientService
         
        public List<ACClient> ConnectedClients { get; } = new List<ACClient>();

        public IHubClients Clients
        {
            get
            {
                return _Context.Clients;
            }
        }

        public async Task SetDriverName(string driverName) =>
            await _Context.Clients.All.SendAsync("SetDriver", driverName);

        public async Task SetCurrentCar(string carId, string skinId) =>
            await _Context.Clients.All.SendAsync("SetCurrentCar", carId, skinId);

        public async Task SetCurrentTrack(string trackId, string layoutId) =>
            await _Context.Clients.All.SendAsync("SetCurrentTrack", trackId, layoutId);

        public async Task SetRemoteServer(ACRemoteConfig remoteConfig) =>
            await _Context.Clients.All.SendAsync("SetRemoteServer", remoteConfig);

        public async Task StartSimulator() =>
            await _Context.Clients.All.SendAsync("Start");

        public async Task SimulatorStatus() =>
            await _Context.Clients.All.SendAsync("SimulatorStatus");

        #endregion

    }
}
