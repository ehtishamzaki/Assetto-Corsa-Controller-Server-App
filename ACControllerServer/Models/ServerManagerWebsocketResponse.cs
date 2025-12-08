using static ACControllerServer.Models.DriverStateMessage;

namespace ACControllerServer.Models
{

    public class ServerManagerWebsocketEventtype
    {
        public int EventType { get; set; }
    }

    public class ServerManagerWebsocketResponse
    {
        public Message Message { get; set; }
    }

    public class Message
    {
        public int CurrentRealtimePosInterval { get; set; }
        public Sessioninfo SessionInfo { get; set; }
        public Trackmapdata TrackMapData { get; set; }
        public Trackinfo TrackInfo { get; set; }
        public DateTime SessionStartTime { get; set; }
        public Connecteddrivers ConnectedDrivers { get; set; }
        public Disconnecteddrivers DisconnectedDrivers { get; set; }
        public Dictionary<string, string> CarIDToGUID { get; set; }
        public long BestLap { get; set; }
        public int DamageMultiplier { get; set; }
    }

    public class Sessioninfo
    {
        public int Version { get; set; }
        public int SessionIndex { get; set; }
        public int CurrentSessionIndex { get; set; }
        public int SessionCount { get; set; }
        public string ServerName { get; set; }
        public string Track { get; set; }
        public string TrackConfig { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int Time { get; set; }
        public int Laps { get; set; }
        public int WaitTime { get; set; }
        public int AmbientTemp { get; set; }
        public int RoadTemp { get; set; }
        public string WeatherGraphics { get; set; }
        public int ElapsedMilliseconds { get; set; }
        public int VisibilityMode { get; set; }
        public int EventType { get; set; }
    }

    public class Trackmapdata
    {
        public float width { get; set; }
        public float height { get; set; }
        public int margin { get; set; }
        public float scale_factor { get; set; }
        public float offset_x { get; set; }
        public float offset_y { get; set; }
        public int drawing_size { get; set; }
        public int padding { get; set; }
    }

    public class Trackinfo
    {
        public string name { get; set; }
        public string city { get; set; }
        public string country { get; set; }
        public string description { get; set; }
        public string[] geotags { get; set; }
        public string length { get; set; }
        public int pitboxes { get; set; }
        public string run { get; set; }
        public string[] tags { get; set; }
        public string width { get; set; }
        public string author { get; set; }
        public object version { get; set; }
        public string url { get; set; }
        public object year { get; set; }
    }

    public class Connecteddrivers
    {
        public Dictionary<string, DriverData> Drivers { get; set; }
        public string[] GUIDsInPositionalOrder { get; set; }
    }

    public class DriverData
    {
        public Carinfo CarInfo { get; set; }
        public int TotalNumLaps { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime LoadedTime { get; set; }
        public int Position { get; set; }
        public string Split { get; set; }
        public int DeltaToBest { get; set; }
        public int DeltaToSelf { get; set; }
        public DateTime LastSeen { get; set; }
        public Lastpos LastPos { get; set; }
        public bool IsInPits { get; set; }
        public int LastPitStop { get; set; }
        public bool DRSActive { get; set; }
        public float NormalisedSplinePos { get; set; }
        public int SteerAngle { get; set; }
        public int StatusBytes { get; set; }
        public bool BlueFlag { get; set; }
        public bool HasCompletedSession { get; set; }
        public int Ping { get; set; }
        public Driverswap DriverSwap { get; set; }
        public Driftinfo DriftInfo { get; set; }
        public Dictionary<string, CarData> Cars { get; set; }

        public class Carinfo
        {
            public int CarID { get; set; }
            public string DriverName { get; set; }
            public string DriverGUID { get; set; }
            public string CarModel { get; set; }
            public string CarSkin { get; set; }
            public string Tyres { get; set; }
            public string DriverInitials { get; set; }
            public string CarName { get; set; }
            public string ClassID { get; set; }
            public int EventType { get; set; }
        }

        public class Lastpos
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        public class Driverswap
        {
            public bool IsInDriverSwap { get; set; }
            public DateTime DriverSwapExpectedCompletionTime { get; set; }
            public bool DriverSwapHadPenalty { get; set; }
            public DateTime DriverSwapActualCompletionTime { get; set; }
        }

        public class Driftinfo
        {
            public int CurrentLapScore { get; set; }
            public int BestLapScore { get; set; }
        }

        public class CarData
        {
            public float TopSpeedThisLap { get; set; }
            public float TopSpeedBestLap { get; set; }
            public string TyreBestLap { get; set; }
            public long BestLap { get; set; }
            public int NumLaps { get; set; }
            public long LastLap { get; set; }
            public DateTime LastLapCompletedTime { get; set; }
            public long TotalLapTime { get; set; }
            public string CarName { get; set; }
            public Bestlapminisector[] BestLapMiniSectors { get; set; }
            public int QualifyingTime { get; set; }

            public class Bestlapminisector
            {
                public long Time { get; set; }
                public int TimeInLap { get; set; }
                public int Lap { get; set; }
            }
        }

    }

    public class Disconnecteddrivers
    {
        public Dictionary<string, DriverData> Drivers { get; set; }
        public object GUIDsInPositionalOrder { get; set; }
    }


    public class DriverStateWebsocketResponse
    {
        public DriverStateMessage Message { get; set; }
    }
    public class DriverStateMessage
    {
        public int CarID { get; set; }
        public PosData Pos { get; set; }
        public VelocityData Velocity { get; set; }
        public RotationData Rotation { get; set; }
        public int Gear { get; set; }
        public int EngineRPM { get; set; }
        public int NormalisedSplinePos { get; set; }
        public int SteerAngle { get; set; }
        public int StatusBytes { get; set; }
        public int RacePosition { get; set; }
        public string Gap { get; set; }
        public int DeltaToBest { get; set; }
        public int DeltaToSelf { get; set; }
        public bool BlueFlag { get; set; }
        public bool IsInPits { get; set; }
        public bool DRSActive { get; set; }
        public bool HasCompletedSession { get; set; }
        public int Ping { get; set; }

        public class PosData
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public class VelocityData
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public class RotationData
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
    }

    public class DriverConnectedWebsocketResponse
    {
        public DriverConnectedMessage Message { get; set; }
    }
    public class DriverConnectedMessage
    {
        public int CarID { get; set; }
        public string DriverName { get; set; }
        public string DriverGUID { get; set; }
        public string CarModel { get; set; }
        public string CarSkin { get; set; }
        public string Tyres { get; set; }
        public string DriverInitials { get; set; }
        public string CarName { get; set; }
        public string ClassID { get; set; }
        public int EventType { get; set; }
    }

}
