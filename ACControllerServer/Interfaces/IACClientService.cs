using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Interfaces
{
    public interface IACClientService
    {
        public List<ACClient> ConnectedClients { get; }

        public IHubClients Clients { get; }

        public Task SetDriverName(string driverName);

        public Task SetCurrentCar(string carId, string skinId);

        public Task SetCurrentTrack(string trackId, string layoutId);

        public Task SetRemoteServer(ACRemoteConfig remoteConfig);

        public Task StartSimulator();
        
        public Task SimulatorStatus();

    }
}
