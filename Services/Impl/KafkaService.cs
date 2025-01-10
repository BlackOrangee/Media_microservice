using Confluent.Kafka;
using Media_microservice.Requests;
using System.Text.Json;

namespace Media_microservice.Services.Impl
{
    public class KafkaService : IKafkaService
    {
        private readonly ILogger<KafkaService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly IProducer<string, string> _responseProducer;
        private readonly IMinioService _minioService;
        private readonly string _topic;

        public KafkaService(ILogger<KafkaService> logger, IMinioService minioService,
                            ConsumerConfig consumerConfig, ProducerConfig producerConfig, 
                            string topic)
        {
            _logger = logger;
            _minioService = minioService;

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _responseProducer = new ProducerBuilder<string, string>(producerConfig).Build();

            _topic = topic;
            _consumer.Subscribe(_topic);
        }

        public async Task StartListeningAsync()
        {
            while (true)
            {
                var consumeResult = _consumer.Consume();
                var fileRequest = JsonSerializer.Deserialize<FileRequest>(consumeResult.Message.Value);

                if (fileRequest != null)
                {
                    switch (fileRequest.Operation)
                    {
                        case "Save":
                            await HandleSaveFileAsync(fileRequest);
                            break;
                        case "Get":
                            await HandleGetFileAsync(fileRequest);
                            break;
                        case "Delete":
                            await HandleDeleteFileAsync(fileRequest);
                            break;
                    }
                }
                _consumer.Commit(consumeResult);
            }
        }

        private async Task HandleSaveFileAsync(FileRequest request)
        {
            if (request.FileData == null)
            {
                throw new ArgumentNullException(nameof(request.FileData));
            }
            var fileData = Convert.FromBase64String(request.FileData);
            await _minioService.SaveFileAsync(request.FileName, fileData);
            _logger.LogInformation($"File {request.FileName} saved");
        }

        private async Task HandleGetFileAsync(FileRequest request)
        {
            int lifeTime = 300;
            if (request.LifeTime != null)
            {
                lifeTime = request.LifeTime.Value;
            }
            var url = await _minioService.GeneratePresignedUrlAsync(request.FileName, lifeTime);
            await SendResponseAsync(request.FileName, url);
            _logger.LogInformation($"Presigned URL for file {request.FileName} generated");
        }

        private async Task HandleDeleteFileAsync(FileRequest request)
        {
            await _minioService.DeleteFileAsync(request.FileName);
            _logger.LogInformation($"File {request.FileName} deleted");
        }

        private async Task SendResponseAsync(string key, string response)
        {
            await _responseProducer.ProduceAsync("media-responses", new Message<string, string>
            {
                Key = key,
                Value = response
            });
        }
    }
}
