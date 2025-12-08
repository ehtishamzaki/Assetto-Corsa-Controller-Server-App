using ACControllerServer.Models;
using System.Collections.Generic;

namespace ACControllerServer.Interfaces
{
    public interface IAcDirectoryService
    {

        public List<CarData> Cars { get; }

        public List<TrackData> Tracks { get; }

    }
}
