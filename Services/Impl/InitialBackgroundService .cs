namespace Media_microservice.Services.Impl
{
    public class InitialBackgroundService : BackgroundService
    {
        private readonly ICommunicationService _communicationService;
        private readonly ILogger<InitialBackgroundService> _logger;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(20);

        public InitialBackgroundService(ICommunicationService communicationService, ILogger<InitialBackgroundService> logger)
        {
            _communicationService = communicationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka listener...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _communicationService.StartListeningAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Listener stopped unexpectedly: {ex.Message}");

                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Stopping listener due to cancellation.");
                        break;
                    }

                    _logger.LogInformation($"Retrying to start listener in {_retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(_retryDelay, stoppingToken);
                }
            }

            _logger.LogInformation("Listener has been stopped.");
        }
    }
}
