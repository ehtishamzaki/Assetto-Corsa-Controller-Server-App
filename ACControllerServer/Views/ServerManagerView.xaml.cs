using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ACControllerServer.Views
{
    /// <summary>
    /// Interaction logic for ServerManagerView.xaml
    /// </summary>
    public partial class ServerManagerView : UserControl, INotifyPropertyChanged
    {
        private readonly IACClientService _clientService;
        private readonly IServerManagerService _serverManager;
        private readonly AssettoCorsa _assettoCorsa;

        public ServerManagerView()
        {
            InitializeComponent();
        }

        public ServerManagerView(IACClientService aCClientService, 
            IServerManagerService serverManager,
            AssettoCorsa assettoCorsa)
        {
            _clientService = aCClientService;
            _serverManager = serverManager;
            _assettoCorsa = assettoCorsa;
            _serverManager.LoginStatusChanged += LoginStatusChanged;
            _serverManager.OnRaceStart += ServerManager_OnRaceStart;
            _serverManager.OnRaceEnd += ServerManager_OnRaceEnd;

            RaceTimeChangeCommand = new RelayCommand(OnRaceTimeChanged);
            InitializeComponent();
        }

        private void ServerManager_OnRaceStart(object sender, EventArgs e)
        {
            IsHosted = true;
            PropertyNofity(nameof(IsHosted));
            IsRaceStarted = true;
            PropertyNofity(nameof(IsRaceStarted));
        }

        private void ServerManager_OnRaceEnd(object sender, EventArgs e)
        {
            if (IsHosted)
            {
                _serverManager.StopHostedEvent().ConfigureAwait(false);
                IsHosted = false;
            }

            IsRaceStarted = false;
            PropertyNofity(nameof(IsHosted));
            PropertyNofity(nameof(IsRaceStarted));
        }

        private void LoginStatusChanged(object sender, EventArgs e)
        {
            PropertyNofity(nameof(IsConnected));
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyNofity(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Properties

        public bool IsConnected { get { return _serverManager.IsLoggedIn; } }

        public IRelayCommand RaceTimeChangeCommand { get; }
        public bool IsTimedRace { get; set; } = false;
        public bool IsLapRace { get; set; } = true;

        public bool IsHosted { get; set; }

        public bool IsRaceStarted { get; set; }

        public string NumOfLaps { get; set; } = "3";

        public string RaceTime { get; set; } = "10";

        #endregion

        private void OnRaceTimeChanged()
        {
            // notify UI
            PropertyNofity(nameof(IsLapRace));
            PropertyNofity(nameof(IsTimedRace));
        }

        private async void btnHostRace_Click(object sender, RoutedEventArgs e)
        {
            // check if race already hosted
            if (!IsHosted)
            {
                // check if any clients are assigned
                if (!_clientService.ConnectedClients.Any(x => x.IsAssigned && x.IsConnected))
                {
                    MessageBox.Show("Please assign atleast one client to start a Race!",
                        "Error Hosting Race", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (IsLapRace)
                    IsHosted = await _serverManager.HostSimpleRace(_assettoCorsa.SelectedTrackData.TrackId, 
                        _assettoCorsa.SelectedLayout.Id,
                        new string[] { _assettoCorsa.SelectedCarData.CarId },
                        raceLaps: NumOfLaps,
                        raceTime: "0",
                        maxClients: _assettoCorsa.SelectedLayout.PitBoxes);
                else
                    IsHosted = await _serverManager.HostSimpleRace(_assettoCorsa.SelectedTrackData.TrackId,
                        _assettoCorsa.SelectedLayout.Id,
                        new string[] { _assettoCorsa.SelectedCarData.CarId },
                        raceTime: RaceTime,
                        raceLaps: "0",
                        maxClients: _assettoCorsa.SelectedLayout.PitBoxes);
            }
            else
                IsHosted = !(await _serverManager.StopHostedEvent());
            // get the remote server config and forward to clients
            if (IsHosted)
            {
                await _clientService.SetRemoteServer(await _serverManager.GetRemoteServerConfig());
                await _clientService.SetCurrentCar(_assettoCorsa.SelectedCarData.CarId, _assettoCorsa.SelectedSkin.SkinId);
                await _clientService.SetCurrentTrack(_assettoCorsa.SelectedTrackData.TrackId, _assettoCorsa.SelectedLayout.Id);
            }
            IsRaceStarted = false;
            PropertyNofity(nameof(IsHosted));
            PropertyNofity(nameof(IsRaceStarted));
        }

        private async void btnStartRace_Click(object sender, RoutedEventArgs e)
        {
            IsRaceStarted = true;
            PropertyNofity(nameof(IsRaceStarted));
            await _clientService.StartSimulator();
        }
    }
}
