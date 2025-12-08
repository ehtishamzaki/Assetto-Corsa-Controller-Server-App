using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    internal class MqttServerConfig
    {

        #region Constant

        public const string Section = "MQTTServer";

        #endregion

        #region Properties

        public string ServerAddress { get; set; }

        public int ServerPort { get; set; }

        public string LoginUsername { get; set; }

        public string LoginPassword { get; set; }

        public string Topic { get; set; }
        
        #endregion

    }
}
