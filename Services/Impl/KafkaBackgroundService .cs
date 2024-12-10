namespace Media_microservice.Services.Impl
{
    public class KafkaBackgroundService : BackgroundService
    {
        private readonly IKafkaService _kafkaService;
        private readonly ILogger<KafkaBackgroundService> _logger;

        public KafkaBackgroundService(IKafkaService kafkaService, ILogger<KafkaBackgroundService> logger)
        {
            _kafkaService = kafkaService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Kafka listener...");

            try
            {
                await _kafkaService.StartListeningAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kafka listener stopped unexpectedly: {ex.Message}");
                throw;
            }
        }
    }
}
