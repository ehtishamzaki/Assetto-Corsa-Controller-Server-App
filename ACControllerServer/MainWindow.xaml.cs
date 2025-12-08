using ACControllerServer.Views;
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
            ServerManagerView serverManagerPage)
        {
            _logger = logger;
            AssettoCorsaPage = assettoCorsaPage;
            ACClientsPage = aCClientsPage;

            ServerManagerPage = serverManagerPage;
            ServerManagerPage.PropertyChanged += ServerManagerPage_PropertyChanged;

            // initialize components
            InitializeComponent();
            _logger.LogInformation("{this} has been initialized, version: {version}", this, _AppVersion);
            Title += $" [ v{_AppVersion} ]";
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
    }
}
