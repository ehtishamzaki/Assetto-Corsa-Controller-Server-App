namespace ACControllerServer.HostedServices
{
    public class AcProcessChecker : BackgroundService
    {

        public AcProcessChecker(ILogger<AcProcessChecker> logger,
            IACClientService acClientService, 
            IHostApplicationLifetime serverAddresses)
        {
            _logger = logger;
            _acClientService = acClientService;

            serverAddresses.ApplicationStarted.Register(ApplicationStartedCallBack);
            _logger.LogDebug("{this} has been initialized.", this);
        }

        #region Variables

        private readonly ILogger<AcProcessChecker> _logger; 
        private readonly IACClientService _acClientService;
        private readonly int _Interval = 5000;

        private bool _IsHostStarted { get; set; } = false;

        #endregion

        private void ApplicationStartedCallBack()
        {
            _IsHostStarted = true;
            _logger.LogDebug("ApplicationStarted callback received for {this}", this);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();
            
            // loop until the cancellation requested
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // check if the application host is started
                    if (!_IsHostStarted)
                        continue;

                    // find it there is process running
                    await _acClientService.SimulatorStatus();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "an error occured while checking for running Process");
                }
                finally
                {
                    await Task.Delay(_Interval, stoppingToken);
                }
            }

        }
    }
}
