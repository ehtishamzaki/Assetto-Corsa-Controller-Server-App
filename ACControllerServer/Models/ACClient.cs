using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Models
{
    public class ACClient
    {
        
        public string Id { get; set; }

        public string Name { get; set; }

        public bool IsConnected { get; set; }

        public bool IsSimulatorRunning { get; set; }

        public bool IsAssigned { get; set; }

        public string AssignedId { get; set; }

        public string AssignedName { get; set; }

    }
}
