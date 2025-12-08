using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    public class EventHouseConfig
    {

        public const string Section = "EventhouseAPI";

        public string ServerAddress { get; set; }

        public string ActivationId { get; set; }

        public string EventToken { get; set; }

        public string ActivationToken { get; set; }

    }
}
