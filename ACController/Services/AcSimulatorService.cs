using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ACController.Services
{
    public class AcSimulatorService
    {

        public AcSimulatorService(string acRoot)
        {
            _AcRoot = Path.GetFullPath(acRoot ?? string.Empty);

            _ProcessName = FindProcessName();
            _ProcessNameWithoutExt = Path.GetFileNameWithoutExtension(_ProcessName);
        }

        #region Variables

        private readonly string _AcRoot;
        private Process _SimulatorProcess;
        private readonly string _ProcessName;
        private readonly string _ProcessNameWithoutExt;

        #endregion

        #region Methods

        private string FindProcessName()
        {
            if (File.Exists(Path.Combine(_AcRoot, "acs_pro.exe")))
                return "acs_pro.exe";
            if (Environment.Is64BitOperatingSystem && File.Exists(Path.Combine(_AcRoot, "acs.exe")))
                return "acs.exe";
            if (!Environment.Is64BitOperatingSystem && File.Exists(Path.Combine(_AcRoot, "acs_x86")))
                return "acs_x86";

            return Path.GetFileName(Directory.GetFiles(_AcRoot, "acs*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault());
        }

        #endregion

        #region IAcSimulatorService

        public bool IsRunning => _SimulatorProcess != null && !_SimulatorProcess.HasExited;

        public void StartSimulator()
        {
            ProcessStartInfo startInfo = new(_ProcessName)
            {
                Arguments = "/spawn",
                WorkingDirectory = _AcRoot,
                Verb = "runas",
                UseShellExecute = true
            };
            _SimulatorProcess = Process.Start(startInfo);
        }

        public void StopSimulator()
        {
            if (_SimulatorProcess != null && !_SimulatorProcess.HasExited)
                _SimulatorProcess.Kill();
        }

        public async Task FindProcess()
        {
            if (IsRunning || string.IsNullOrWhiteSpace(_ProcessName))
                return;
            if (_SimulatorProcess != null && _SimulatorProcess.HasExited)
                _SimulatorProcess = null;

            Process[] allProcess = await Task.Run(Process.GetProcesses);
            _SimulatorProcess = allProcess.FirstOrDefault(x => x.ProcessName == _ProcessNameWithoutExt);

            Array.Clear(allProcess);
        }

        #endregion

    }
}