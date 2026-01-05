using ACControllerServer.Views;
using ACControllerServer.Services;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ACControllerServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow(ILogger<MainWindow> logger,
            AssettoCorsa assettoCorsaPage,
            ACClientsView aCClientsPage,
            ServerManagerView serverManagerPage,
            WakeOnLanService wolService,
            IACClientService clientService,
            IOptions<ACServerConfig> config)
        {
            _logger = logger;
            _wolService = wolService;
            _clientService = clientService;
            _config = config.Value;

            AssettoCorsaPage = assettoCorsaPage;
            ACClientsPage = aCClientsPage;
            ServerManagerPage = serverManagerPage;
            ServerManagerPage.PropertyChanged += ServerManagerPage_PropertyChanged;

            InitializeComponent();
            _logger.LogInformation("{this} has been initialized, version: {version}", this, _AppVersion);
            Title += $" [ v{_AppVersion} ]";

            btnPowerOnAll.Click += btnPowerOnAll_Click;
            btnPowerOffAll.Click += btnPowerOffAll_Click;
        }

        private void ServerManagerPage_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ServerManagerPage.IsHosted))
            {
                Dispatcher.Invoke(() =>
                {
                    AssettoCorsaPage.IsEnabled = !ServerManagerPage.IsHosted;
                    ACClientsPage.IsEnabled = !ServerManagerPage.IsHosted;
                });
            }
        }

        #region Variables

        private readonly ILogger<MainWindow> _logger;
        private readonly string _AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        private readonly WakeOnLanService _wolService;
        private readonly IACClientService _clientService;
        private readonly ACServerConfig _config;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void PropertyNofity(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Properties

        public ACClientsView ACClientsPage { get; }
        public AssettoCorsa AssettoCorsaPage { get; }
        public ServerManagerView ServerManagerPage { get; }

        #endregion

        #region Power Control

        private async void btnPowerOnAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_config.SimulatorsMacAddressList == null || _config.SimulatorsMacAddressList.Count == 0)
                {
                    MessageBox.Show("No MAC addresses configured.\n\nPlease add MAC addresses to SimulatorsMacAddressList in appsettings.json",
                        "Configuration Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int count = await _wolService.WakeDevices(_config.SimulatorsMacAddressList);
                _logger.LogInformation("Wake-on-LAN sent to {count} devices", count);

                MessageBox.Show($"Wake-on-LAN packets sent to {count} simulator(s).\n\nPlease wait for the PCs to boot up.",
                    "Power On", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Wake-on-LAN packets");
                MessageBox.Show($"Failed to send Wake-on-LAN: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnPowerOffAll_Click(object sender, RoutedEventArgs e)
        {
            if (_clientService.ConnectedClients == null || _clientService.ConnectedClients.Count == 0)
            {
                MessageBox.Show("No simulators are currently connected.",
                    "Power Off", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to shut down {_clientService.ConnectedClients.Count} connected simulator(s)?",
                "Confirm Power Off", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _clientService.StopSimulator();
                    _logger.LogInformation("Power off command sent to all connected simulators");

                    MessageBox.Show("Shutdown command sent to all connected simulators.",
                        "Power Off", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send power off command");
                    MessageBox.Show($"Failed to send power off command: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}