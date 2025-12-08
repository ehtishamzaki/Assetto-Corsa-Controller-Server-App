using ACController.Models;
using System;
using System.IO;
using System.Windows.Controls.Primitives;

namespace ACController.Services
{
    public class AcConfigService
    {

        public AcConfigService()
        {
            // readonly variables
            _AcRootDocuments = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Assetto Corsa");
            if (!Directory.Exists(_AcRootDocuments))
                throw new DirectoryNotFoundException("No Assetto Corsa directory found in document. Please run the AC_Pro for first time.");

            // initialize the race ini file
            _RaceIniFile = new(Path.Combine(_AcRootDocuments, "cfg", "race.ini"));
            if (!_RaceIniFile.Exists)
                throw new FileNotFoundException("Race.ini file doesn't found in documents folder.", _RaceIniFile.Path);
        }

        #region Variables

        private readonly string _AcRootDocuments;

        private readonly IniFile _RaceIniFile;
        private readonly object _lockRaceFile = new();

        #endregion

        #region IAcConfigService

        public object GetDriverName()
        {
            lock (_lockRaceFile)
            {
                return new { DriverName = _RaceIniFile.IniReadValue("CAR_0", "DRIVER_NAME", string.Empty) };
            }
        } 
        public bool SetDriverName(string driverName)
        {
            lock (_lockRaceFile)
            {
                _RaceIniFile.IniWriteValue("CAR_0", "DRIVER_NAME", driverName);
                _RaceIniFile.IniWriteValue("REMOTE", "NAME", driverName);
                return true;
            }
        }

        public object GetCarDetails()
        {
            lock (_lockRaceFile)
            {
                return new { 
                    CarId = _RaceIniFile.IniReadValue("RACE", "MODEL", string.Empty), 
                    SkinId = _RaceIniFile.IniReadValue("RACE", "SKIN", string.Empty)
                };
            }
        }
        public bool SetCarDetails(string carId, string skinId)
        {
            lock (_lockRaceFile)
            {
                _RaceIniFile.IniWriteValue("REMOTE", "REQUESTED_CAR", carId);
                _RaceIniFile.IniWriteValue("CAR_0", "SKIN", skinId);
                _RaceIniFile.IniWriteValue("RACE", "MODEL", carId);
                _RaceIniFile.IniWriteValue("RACE", "SKIN", skinId);
                return true;
            }
        }

        public object GetTrackDetails()
        {
            lock (_lockRaceFile)
            {
                return new 
                {
                    TrackId = _RaceIniFile.IniReadValue("RACE", "TRACK", string.Empty),
                    LayoutId = _RaceIniFile.IniReadValue("RACE", "CONFIG_TRACK", string.Empty)
                };
            }
        }
        public bool SetTrackDetails(string trackId, string layoutId)
        {
            lock (_lockRaceFile)
            {
                _RaceIniFile.IniWriteValue("RACE", "TRACK", trackId);
                if (layoutId == "ui")
                    _RaceIniFile.IniWriteValue("RACE", "CONFIG_TRACK", string.Empty);
                else
                    _RaceIniFile.IniWriteValue("RACE", "CONFIG_TRACK", layoutId);
                return true;
            }
        }

        public bool SetRemoteServer(ACRemoteConfig remoteConfig)
        {
            lock (_lockRaceFile)
            {
                _RaceIniFile.IniWriteValue("REMOTE", "ACTIVE", remoteConfig == null ? "0" : "1");
                if (remoteConfig == null)
                    return false;
                _RaceIniFile.IniWriteValue("REMOTE", "GUID", Guid.NewGuid().ToString());
                _RaceIniFile.IniWriteValue("REMOTE", "SERVER_IP", remoteConfig.ip);
                _RaceIniFile.IniWriteValue("REMOTE", "SERVER_PORT", remoteConfig.port);
                _RaceIniFile.IniWriteValue("REMOTE", "SERVER_NAME", remoteConfig.name);
                _RaceIniFile.IniWriteValue("REMOTE", "SERVER_HTTP_PORT", remoteConfig.cport);
                return true;
            }
        }

        #endregion

    }
}
