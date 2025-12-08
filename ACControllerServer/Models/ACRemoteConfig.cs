using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{

    public class ACRemoteConfig
    {
        public string ip { get; set; }
        public int port { get; set; }
        public int tport { get; set; }
        public int cport { get; set; }
        public string name { get; set; }
        public int clients { get; set; }
        public int maxclients { get; set; }
        public string track { get; set; }
        public string[] cars { get; set; }
        public int timeofday { get; set; }
        public int session { get; set; }
        public int[] sessiontypes { get; set; }
        public int[] durations { get; set; }
        public int timeleft { get; set; }
        public string[] country { get; set; }
        public bool pass { get; set; }
        public int timestamp { get; set; }
        public object json { get; set; }
        public bool l { get; set; }
        public bool pickup { get; set; }
        public bool timed { get; set; }
        public bool extra { get; set; }
        public bool pit { get; set; }
        public int inverted { get; set; }

    }
}
