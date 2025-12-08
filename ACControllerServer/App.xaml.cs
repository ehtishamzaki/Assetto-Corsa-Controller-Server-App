using Serilog;
using System.Windows;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace ACControllerServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IACClientService _clientService;
        private IHost _Host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // setup host server
            _Host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureLogging(config => config.ClearProviders())
                .UseSerilog()
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseStartup<Startup>();
                })
                .Build();
            // startup UI
            _clientService = _Host.Services.GetRequiredService<IACClientService>();
            this.MainWindow = _Host.Services.GetService<MainWindow>();
            this.MainWindow.Show();
            // start the host
            _Host.RunAsync().ConfigureAwait(false);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // unassign all the simulators
            await _clientService.SetDriverName(string.Empty);
            // stop the server host
            await _Host?.StopAsync();
            _Host?.Dispose();
            base.OnExit(e);
        }
    }
}
