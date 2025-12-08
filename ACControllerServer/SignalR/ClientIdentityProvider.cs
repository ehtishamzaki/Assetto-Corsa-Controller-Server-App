using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACControllerServer.SignalR
{
    public class ClientIdentityProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            var steamId = connection.GetHttpContext().Request.Query["steamId"]!.ToString();
            return steamId;
        }
    }
}
