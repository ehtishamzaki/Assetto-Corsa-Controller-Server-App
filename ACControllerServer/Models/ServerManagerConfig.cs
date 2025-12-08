using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    public class ServerManagerConfig
    {
        public const string Section = "ServerManager";

        public string ServerAddress { get; set; }

        public string LoginUsername { get; set; }

        public string LoginPassword { get; set; }

    }
}
