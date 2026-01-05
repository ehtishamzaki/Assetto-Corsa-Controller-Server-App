using ACControllerServer.HostedServices;
using ACControllerServer.SignalR;
using ACControllerServer.Views;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ACControllerServer
{
    public class Startup
    {

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSignalR();

            // configure options
            services.Configure<ConsoleLifetimeOptions>(options => options.SuppressStatusMessages = true);
            services.Configure<ACServerConfig>(Configuration.GetSection(ACServerConfig.Section));
            services.Configure<ServerManagerConfig>(Configuration.GetSection(ServerManagerConfig.Section));
            services.Configure<EventHouseConfig>(Configuration.GetSection(EventHouseConfig.Section));
            services.Configure<MqttServerConfig>(Configuration.GetSection(MqttServerConfig.Section));

            // configure views
            services.AddSingleton<MainWindow>();
            services.AddSingleton<AssettoCorsa>();
            services.AddSingleton<ACClientsView>();
            services.AddSingleton<ServerManagerView>();

            // configure services
            services.AddSingleton<WakeOnLanService>();
            services.AddTransient<ClientHub>();
            services.AddSingleton<IMqttHandlerService, MqttHandlerService>();
            services.AddSingleton<IEventHouseAPI, EventHouseAPI>();
            services.AddSingleton<IServerManagerService, ServerManagerService>();
            services.AddSingleton<IACClientService, ACClientService>();
            services.AddSingleton<IUserIdProvider, ClientIdentityProvider>();
            services.AddSingleton<IAcDirectoryService, AcDirectoryService>();

            // configure hosted services
            services.AddHostedService<AcProcessChecker>();
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseAuthorization();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ClientHub>("/api/signalr");
                endpoints.MapControllers();
            });
        }
    }
}
