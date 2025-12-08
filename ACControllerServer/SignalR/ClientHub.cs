using ACControllerServer.Views;
using Microsoft.AspNetCore.SignalR;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace ACControllerServer.SignalR
{
    public class ClientHub : Hub
    {
        private readonly ILogger<ClientHub> _logger;
        private readonly IACClientService _clientService;
        private readonly ACClientsView _clientsView;

        public ClientHub(ILogger<ClientHub> logger, 
            IACClientService clientService,
            ACClientsView clientsView)
        {
            _logger = logger;
            _clientService = clientService;
            _clientsView = clientsView;

            _logger.LogDebug("{this} has been initialized", this);
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("client '{ip}' is connected!", Context.UserIdentifier);
            var assignedName = Context.GetHttpContext().Request.Query["name"]!.ToString();
            var client = _clientService.ConnectedClients.FirstOrDefault(x => x.Id == Context.UserIdentifier);
            if (client == null)
                _clientService.ConnectedClients.Add(new ACClient()
                {
                    Id = Context.UserIdentifier,
                    Name = assignedName,
                    IsConnected = true,
                    IsAssigned = false,
                    IsSimulatorRunning = false,
                    AssignedName = string.Empty,
                });
            else
            {
                client.Name = assignedName;
                client.IsAssigned = false;
                client.IsConnected = true;
                client.IsSimulatorRunning = false;
                client.AssignedName = string.Empty;
            }
            _clientsView.ClientsChanged();
            Clients.Caller.SendAsync("SimulatorStatus").ConfigureAwait(false);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_clientService.ConnectedClients.Any(x => x.Id == Context.UserIdentifier))
            {
                var client = _clientService.ConnectedClients.First(x => x.Id == Context.UserIdentifier);
                client.IsConnected = false;
                client.IsAssigned = false;
                client.IsSimulatorRunning = false;
                client.AssignedName = string.Empty;
            }
            _clientsView.ClientsChanged();
            _logger.LogInformation("client '{ip}' is disconnected!", Context.UserIdentifier);
            return base.OnDisconnectedAsync(exception);
        }

        public void UpdateStatus(bool IsRunning)
        {
            var client = _clientService.ConnectedClients.FirstOrDefault(x => x.Id == Context.UserIdentifier);
            if (client != null)
                client.IsSimulatorRunning = IsRunning;
            else
                _logger.LogWarning("client doesn't found with '{id}' Id.", Context.UserIdentifier);
            _clientsView.ClientsChanged();
        }

    }
}
