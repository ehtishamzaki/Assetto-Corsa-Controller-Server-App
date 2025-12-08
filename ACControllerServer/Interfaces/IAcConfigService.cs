namespace ACControllerServer.Interfaces
{
    public interface IAcConfigService
    {

        public object GetDriverName();
        public object GetCarDetails();
        public object GetTrackDetails();

        public bool SetDriverName(string driverName);
        public bool SetCarDetails(string carId, string skinId);
        public bool SetTrackDetails(string trackId, string layoutId);

    }
}
