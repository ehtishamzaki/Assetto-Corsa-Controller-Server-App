using System.Threading.Tasks;

namespace ACControllerServer.Interfaces
{
    public interface IAcSimulatorService
    {

        public bool IsRunning { get; }
        public void StartSimulator();

        public Task FindProcess();

    }
}
