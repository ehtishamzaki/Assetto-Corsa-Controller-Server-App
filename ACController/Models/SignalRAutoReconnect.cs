using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACController.Models
{
    public class SignalRAutoReconnect : IRetryPolicy
    {
        private readonly int _reconnectInterval;

        /// <summary>
        /// Initialize a custom reconnect interval policy for SignalR
        /// This will prevent a final failure for SignalR and reconnect
        /// after every specified number of seconds
        /// </summary>
        /// <param name="reconnectInterval">Reconnect interval in seconds</param>
        public SignalRAutoReconnect(int reconnectInterval)
        {
            _reconnectInterval = reconnectInterval;
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(_reconnectInterval);
        }
    }
}
