using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    /// Interaction logic for AssettoCorsa.xaml
    /// </summary>
    public partial class AssettoCorsa : UserControl, INotifyPropertyChanged
    {

        public AssettoCorsa()
        {
            InitializeComponent();
        }

        public AssettoCorsa(ILogger<AssettoCorsa> logger,
            IAcDirectoryService acContent)
        {
            _logger = logger;
            _AcContent = acContent;

            InitializeComponent();
            // setup context
            SelectedCarData = _AcContent.Cars.FirstOrDefault();
            SelectedSkin = SelectedCarData?.Skins.FirstOrDefault();
            SelectedTrackData = _AcContent.Tracks.FirstOrDefault();
            SelectedLayout = SelectedTrackData?.Layouts.FirstOrDefault();
            
            comboCars.SelectedIndex = 0; comboTracks.SelectedIndex = 0;
            _logger.LogDebug("{this} has been initialized", this);
        }

        #region Variables

        private readonly ILogger<AssettoCorsa> _logger;
        private readonly IAcDirectoryService _AcContent;

        public JsonElement ServerCar;
        public JsonElement ServerTrack;
        public JsonElement DriverName;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void PropertyNofity(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Properties

        public List<CarData> Cars { get { return _AcContent.Cars; } }
        public CarData SelectedCarData { get; set; } 
        public CarSkinData SelectedSkin { get; set; }
        public ImageSource CarImage { get; set; }

        public List<TrackData> Tracks { get { return _AcContent.Tracks; } }
        public TrackData SelectedTrackData { get; set; }
        public TrackLayout SelectedLayout { get; set; }
        public ImageBrush TrackBackground { get; set; } = new ImageBrush() { Stretch = Stretch.Uniform };
        public ImageSource TrackOutlineImage { get; set; }

        #endregion


        private void comboCars_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyNofity(nameof(SelectedCarData));
            // check if value not null
            if (comboCars.SelectedValue != null)
                comboSkins.SelectedIndex = 0;
        }

        private void comboSkins_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboSkins.SelectedValue != null)
            {
                // set the image file path
                CarImage = new BitmapImage(new Uri(SelectedSkin.SkinPreviewImage));
                PropertyNofity(nameof(CarImage));
            }
        }

        private void comboTracks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PropertyNofity(nameof(SelectedTrackData));
            if (comboTracks.SelectedValue != null)
                comboLayouts.SelectedIndex = 0;
        }

        private void comboLayouts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboLayouts.SelectedValue != null)
            {
                // change the track background
                TrackBackground.ImageSource = new BitmapImage(new Uri(SelectedLayout.ThumbnailImage));
                PropertyNofity(nameof(TrackBackground));

                // change the track outline
                TrackOutlineImage = new BitmapImage(new Uri(SelectedLayout.OutlineMapImage));
                PropertyNofity(nameof(TrackOutlineImage));
            }
        }

    }
}
