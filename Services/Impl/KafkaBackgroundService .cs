namespace Media_microservice.Services.Impl
{
    public class KafkaBackgroundService : BackgroundService
    {
        private readonly IKafkaService _kafkaService;
        private readonly ILogger<KafkaBackgroundService> _logger;
        private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);

        public KafkaBackgroundService(IKafkaService kafkaService, ILogger<KafkaBackgroundService> logger)
        {
            _kafkaService = kafkaService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka listener...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _kafkaService.StartListeningAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Kafka listener stopped unexpectedly: {ex.Message}");

                    if (stoppingToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Stopping Kafka listener due to cancellation.");
                        break;
                    }

                    _logger.LogInformation($"Retrying to start Kafka listener in {_retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(_retryDelay, stoppingToken);
                }
            }

            _logger.LogInformation("Kafka listener has been stopped.");
        }
    }
}
