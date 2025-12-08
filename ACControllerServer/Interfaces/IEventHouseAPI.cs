using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.Interfaces
{
    public interface IEventHouseAPI
    {

        public Task<EventHouseVisitorResponse> GetVisitor(string qrCodeData);

        public Task<bool> PostVisitorResults(
            string visitor_id,
            string bestlapTimeInMs,
            string trackId,
            string trackName,
            string carId,
            string carName);

    }
}
