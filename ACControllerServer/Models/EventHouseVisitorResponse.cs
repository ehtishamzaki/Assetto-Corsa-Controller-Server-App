using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    public class EventHouseVisitorResponse
    {
        public string VisitorID { get; set; }
        public string VisitorName { get; set; }

        public bool HaveError { get { return response == "error"; } }
        public string response { get; set; }
        public string message { get; set; }
    }

}
