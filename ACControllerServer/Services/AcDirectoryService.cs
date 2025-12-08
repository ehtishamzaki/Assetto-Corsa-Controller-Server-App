using Microsoft.Extensions.Options;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace ACControllerServer.Services
{
    public class AcDirectoryService : IAcDirectoryService
    {

        public AcDirectoryService(ILogger<AcDirectoryService> logger,
            IOptions<ACServerConfig> acServerConfig) 
        { 
            // readonly variables
            _logger = logger;
            _CarsDirectory = Path.GetFullPath(acServerConfig.Value.CarsDirectory ?? ".");
            _TracksDirectory = Path.GetFullPath(acServerConfig.Value.TracksDirectory ?? ".");

            // validate the AC root directory
            if (!Directory.Exists(_CarsDirectory))
            {
                _logger.LogCritical("Cars '{dir}' directory doesn't exist!", _CarsDirectory);
                throw new DirectoryNotFoundException("Unable to find the Assetto Corsa cars directory.");
            }
            if (!Directory.Exists(_TracksDirectory))
            {
                _logger.LogCritical("Tracks '{dir}' directory doesn't exist!", _CarsDirectory);
                throw new DirectoryNotFoundException("Unable to find the Assetto Corsa tracks directory.");
            }
            
            LoadCars(); 
            LoadTracks();
        }

        #region Variables

        private readonly ILogger<AcDirectoryService> _logger;

        private static readonly Regex SplitWordsRegex = new Regex("[\\s_]+|(?<=[a-z])-?(?=[A-Z])|(?<=[a-z]{2})-?(?=[A-Z\\d])", RegexOptions.Compiled);
        private static readonly Regex RejoinRegex = new Regex("(?<=\\bgt) (?=[234]\\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex UpperRegex = new Regex("\\b[a-z]", RegexOptions.Compiled);
        private static readonly Regex UpperAcronimRegex = new Regex("\\b(?:\r\n                [234]d|a(?:i|c(?:c|fl)?|mg)|b(?:r|mw)|c(?:cgt|pu|sl|ts|tr\\d*)|d(?:mc|rs|s3|tm)|\r\n                g(?:gt|pu|rm|sr|t[\\dbceirsx]?\\d?)|f(?:[ox]|fb)|\r\n                h(?:dr|ks|rt)|ita|jtc|ks(?:drift)?|lms?|\r\n                m(?:c12|gu|p\\d*)|n(?:a|vidia)|rs[\\drs]?|s(?:[lm]|r8)|qv|v[dr]c|wrc)\\b", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private readonly string _AcRoot;

        #endregion

        #region Methods

        private readonly string _CarsDirectory;
        private readonly string _TracksDirectory;
        public string DecodeDescription(string data)
        {
            // check if valid string
            if (string.IsNullOrWhiteSpace(data))
                return data;
            // fix the file description
            data = Regex.Replace(data, "<\\s*/?\\s*br\\s*/?\\s*>\\s*", "\n", RegexOptions.Compiled);
            data = Regex.Replace(data, "<\\s*\\w+(\\s+[^>]*|\\s*)>|</\\s*\\w+\\s*>|\\t", string.Empty, RegexOptions.Compiled);
            return HttpUtility.HtmlDecode(data).Trim();
        }
        private string GetNameFromId(in string id)
        {
            // replace the digits and underscores in name
            string parsedName = Regex.Replace(id, "^\\d\\d?_", "");
            if (string.IsNullOrWhiteSpace(parsedName))
                return id;
            // upper case the words in name
            parsedName = SplitWordsRegex.Replace(parsedName, " ");
            parsedName = RejoinRegex.Replace(parsedName, string.Empty);
            parsedName = UpperAcronimRegex.Replace(parsedName, string.Empty);
            parsedName = UpperRegex.Replace(parsedName, (Match x) => x.Value.ToUpper());
            return parsedName.Trim();
        }

        private void LoadCars()
        {
            // get all cars list
            Cars = Directory.EnumerateDirectories(_CarsDirectory)
                .Select(carDirectory => {
                    
                    // get the car details file path
                    string carUiFilePath = Path.Combine(carDirectory, "ui", "ui_car.json");
                    // check if directory exists
                    if (!File.Exists(carUiFilePath))
                        return null;

                    // load the json and deserialize the file data
                    string carUiFileData = File.ReadAllText(carUiFilePath, System.Text.Encoding.UTF8);
                    CarData carData = Newtonsoft.Json.JsonConvert.DeserializeObject<CarData>(carUiFileData);
                    // append custom data
                    carData.CarId = Path.GetFileName(carDirectory);
                    carData.CarDirectory = carDirectory;
                    if (!string.IsNullOrWhiteSpace(carData.CarDescription))
                        carData.CarDescription = DecodeDescription(carData.CarDescription);
                    // check if car name is valid
                    if (string.IsNullOrWhiteSpace(carData.CarName))
                        carData.CarName = GetNameFromId(carData.CarId);

                    return carData;
                })
                .Where(car => car != null)
                .ToList();

            // load all cars skin
            Cars.ForEach(car =>
            {
                string skinsDirectory = Path.Combine(car.CarDirectory, "skins");

                car.Skins = Directory.EnumerateDirectories(skinsDirectory)
                    .Select(skinDirectory =>
                    {
                        // get the skin ui file path
                        string skinUiFilePath = Path.Combine(skinDirectory, "ui_skin.json");
                        if (!File.Exists(skinUiFilePath))
                            return null;
                        // deserialize the skin file
                        CarSkinData skinData = Newtonsoft.Json.JsonConvert
                            .DeserializeObject<CarSkinData>(File.ReadAllText(skinUiFilePath, System.Text.Encoding.UTF8));
                        // add custom data in object
                        skinData.SkinId = Path.GetFileName(skinDirectory);
                        skinData.SkinDirectory = skinDirectory;
                        skinData.SkinPreviewImage = Path.Combine(skinDirectory, "preview.jpg");
                        // check if skin have valid name
                        if (string.IsNullOrWhiteSpace(skinData.SkinName))
                            skinData.SkinName = GetNameFromId(skinData.SkinId);

                        return skinData;
                    })
                    .Where(skin => skin != null)
                    .ToList();
            });

            _logger.LogInformation("Cars data loaded successfully");
        }
        private void LoadTracks()
        {
            Tracks = Directory.EnumerateDirectories(_TracksDirectory)
                .Select(trackDir =>
                {
                    // initialize instance of track data
                    TrackData trackData = new()
                    {
                        TrackId = Path.GetFileName(trackDir),
                        TrackDirectory = trackDir,
                    };
                    trackData.TrackName = GetNameFromId(trackData.TrackId);

                    // get the skin ui file path
                    string trackUiDirectory = Path.Combine(trackDir, "ui");

                    IEnumerable<string> layoutDirectories = Enumerable.Empty<string>();
                    // check if track have multiple layouts
                    if (File.Exists(Path.Combine(trackUiDirectory, "ui_track.json")))
                        layoutDirectories = layoutDirectories.Prepend(trackUiDirectory);
                    else
                        layoutDirectories = Directory.EnumerateDirectories(trackUiDirectory);

                    // loop through each layout 
                    trackData.Layouts = layoutDirectories
                        .Select(layoutDir =>
                        {
                            // get the file path
                            string trackUiFilePath = Path.Combine(layoutDir, "ui_track.json");
                            // check if valid file
                            if (!File.Exists(trackUiFilePath))
                                return null; 

                            // deserialize the layout file
                            TrackLayout layoutData = Newtonsoft.Json.JsonConvert
                                .DeserializeObject<TrackLayout>(File.ReadAllText(trackUiFilePath, System.Text.Encoding.UTF8));
                            // append additional data
                            layoutData.Id = Path.GetFileName(layoutDir);
                            layoutData.LayoutDirectory = layoutDir;
                            layoutData.ThumbnailImage = Path.Combine(layoutDir, "preview.png");
                            layoutData.OutlineMapImage = Path.Combine(layoutDir, "outline.png");
                            // return layout
                            return layoutData;
                        })
                        .Where(layout => layout != null)
                        .ToList();

                    return trackData;
                })
                .Where(track => track != null)
                .ToList();

            _logger.LogInformation("Tracks data loaded successfully");
        }

        #endregion

        #region IAcDirectoryService

        public List<CarData> Cars { get; private set; }

        public List<TrackData> Tracks { get; private set; }

        #endregion

    }
}
