using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ACControllerServer.Views
{
    /// <summary>
    /// Interaction logic for ACClientsView.xaml
    /// </summary>
    public partial class ACClientsView : UserControl, INotifyPropertyChanged
    {
        private readonly ILogger<ACClientsView> _logger;
        private readonly IACClientService _clientService;
        private readonly IEventHouseAPI _eventHouseAPI;

        public ACClientsView()
        {
            AssignCommand = new AsyncRelayCommand<string>(AssignClick_Callback);
            InitializeComponent();
        }
        public ACClientsView(ILogger<ACClientsView> logger,
            IACClientService clientService,
            IEventHouseAPI eventHouseAPI)
        {
            _logger = logger;
            _clientService = clientService;
            _eventHouseAPI = eventHouseAPI;
            InitializeComponent();

            AssignCommand = new AsyncRelayCommand<string>(AssignClick_Callback);
            ClearAssignCommand = new AsyncRelayCommand<string>(ClearAssignment_Callback);
        }

        public void ClientsChanged() => 
            PropertyNofity(nameof(Clients));

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void PropertyNofity(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Properties

        public ObservableCollection<ACClient> Clients
        {
            get
            {
                return new ObservableCollection<ACClient>(_clientService.ConnectedClients);
            }
        }

        public IAsyncRelayCommand<string> AssignCommand { get; set; }

        public IAsyncRelayCommand<string> ClearAssignCommand { get; set; }

        #endregion

        private async Task AssignClick_Callback(string clientId)
        {
            // show the scan qr window
            ScanQRPassWindow scanQRWindow = new();
            if ((bool)!scanQRWindow.ShowDialog())
                return;
            if (string.IsNullOrWhiteSpace(scanQRWindow.txtQRCode.Text))
                return;

            string qrCode = scanQRWindow.txtQRCode.Text.Trim();
            _logger.LogInformation("QR code is scanned for {client}: {value}", clientId, qrCode);
            // find the client in the list
            var client = _clientService.ConnectedClients.First(x => x.Id == clientId);
            // check if client found
            if (client == null)
            {
                _logger.LogError("unable to find the '{client}' for assigning.", clientId);
                MessageBox.Show("Unable to find the connected client in the list", 
                    "Assetto Corsa Clients", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // get the visitor name from the EV API
#if DEBUG
            //var visitor = await _eventHouseAPI.GetVisitor("https://eventshouse.dev/visitors/myprofile?id=43c727ee4fc7250574d2ef90cfa16626388a10e1b30d36ece1c272953ad2ed9e");
            EventHouseVisitorResponse visitor = new()
            {
                response = string.Empty,
                VisitorID = "786",
                VisitorName = "HUSNAIN"
            };
#else
            var visitor = await _eventHouseAPI.GetVisitor(qrCode);
#endif
            if (visitor == null)
            {
                _logger.LogError("The event house API returned invalid response for '{client}' QR", qrCode);
                MessageBox.Show("The event house API returned an invalid response",
                    "Assetto Corsa Clients", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (visitor.HaveError)
            {
                _logger.LogWarning("The event house API returned error for '{client}' QR, Response: {@visitor}", qrCode, visitor);
                MessageBox.Show($"The event house API returned the following response: \n\n{visitor.message}",
                    "Assetto Corsa Clients", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _logger.LogInformation("Event House API response for '{client}' QR, Response: {@visitor}", qrCode, visitor);
            // get the client from SignalR and send the update message to only client
            var clientHub = _clientService.Clients.User(clientId);
            if (clientHub != null)
            {
                await clientHub.SendCoreAsync("SetDriver", new object[] { visitor.VisitorName });
                client.AssignedId = visitor.VisitorID;
                client.AssignedName = visitor.VisitorName;
                client.IsAssigned = true;
                _logger.LogInformation("Assigned '{AssignedName}' name to '{client}' client.", visitor.VisitorName, clientId);
            }
            
            ClientsChanged();
        }
    
        private async Task ClearAssignment_Callback(string clientId)
        {
            if (MessageBox.Show("Are you sure to remove the visitor?", 
                "Assetto Corsa Clients", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;
            // find the client in the list
            var client = _clientService.ConnectedClients.First(x => x.Id == clientId);
            // get the client from SignalR and send the update message to only client
            var clientHub = _clientService.Clients.User(clientId);
            if (clientHub != null)
            {
                await clientHub.SendCoreAsync("SetDriver", new object[] { string.Empty });
                client.AssignedId = string.Empty;
                client.AssignedName = string.Empty;
                client.IsAssigned = false;
                _logger.LogInformation("Clear assignment for '{AssignedName}' on '{client}' client.", client.AssignedName, clientId);
                ClientsChanged();
            }
        }
    }
}
