using Confluent.Kafka;
using Media_microservice.Requests;
using System.Text.Json;

namespace Media_microservice.Services.Impl
{
    public class KafkaService : IKafkaService
    {
        private readonly ILogger<KafkaService> _logger;
        private readonly IConsumer<string, string> _consumer;
        private readonly IMinioService _minioService;
        private readonly string _topic;

        public KafkaService(ILogger<KafkaService> logger, IMinioService minioService,
                                        ConsumerConfig consumerConfig, string topic)
        {
            _logger = logger;
            _minioService = minioService;

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
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
                        case "SaveFile":
                            await HandleSaveFileAsync(fileRequest);
                            break;
                        case "GetFile":
                            await HandleGetFileAsync(fileRequest);
                            break;
                        case "DeleteFile":
                            await HandleDeleteFileAsync(fileRequest);
                            break;
                    }
                }
            }
        }

        private async Task HandleSaveFileAsync(FileRequest request)
        {
            if (request.FileData == null)
            {
                throw new ArgumentNullException(nameof(request.FileData));
            }
            var fileData = Convert.FromBase64String(request.FileData);
            await _minioService.SaveFileAsync(request.Holder, request.FileName, fileData);
            _logger.LogInformation($"File {request.FileName} saved with holder {request.Holder}");
        }

        private async Task HandleGetFileAsync(FileRequest request)
        {
            await _minioService.GeneratePresignedUrlAsync(request.Holder, request.FileName, 300);
            _logger.LogInformation($"Presigned URL for file {request.FileName} generated for holder {request.Holder}");
        }

        private async Task HandleDeleteFileAsync(FileRequest request)
        {
            await _minioService.DeleteFileAsync(request.Holder, request.FileName);
            _logger.LogInformation($"File {request.FileName} deleted with holder {request.Holder}");
        }
    }
}
