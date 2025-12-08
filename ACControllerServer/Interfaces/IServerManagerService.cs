using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Interfaces
{
    public interface IServerManagerService
    {
        public event EventHandler LoginStatusChanged;
        public event EventHandler OnRaceStart;
        public event EventHandler OnRaceEnd;

        public bool IsLoggedIn { get; }

        public Task<ACRemoteConfig> GetRemoteServerConfig();

        public Task<bool> HostQuickRace(string trackId, string layoutId, string[] carIds,
            string qualifyTime = "10", string raceTime = "10", string raceLaps = "0");
        public Task<bool> HostSimpleRace(string trackId, string layoutId, string[] carIds,
            string raceTime = "10", string raceLaps = "0", string maxClients = "2");

        public Task<bool> StopHostedEvent();

    }
}
