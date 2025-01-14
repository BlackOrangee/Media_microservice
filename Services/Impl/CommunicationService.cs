using Media_microservice.Entity;
using Media_microservice.Requests;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Media_microservice.Services.Impl
{
    public class CommunicationService : ICommunicationService
    {
        private readonly ILogger<CommunicationService> _logger;
        private readonly IMinioService _minioService;
        private readonly ApplicationDBContext _context;
        private readonly int _delay = 1000;

        public CommunicationService(ILogger<CommunicationService> logger, IMinioService minioService, ApplicationDBContext context)
        {
            _logger = logger;
            _minioService = minioService;
            _context = context;
        }

        public async Task StartListeningAsync()
        {
            int retryCount = 0;

            while (true)
            {
                if (retryCount > 20)
                {
                    Thread.Sleep(10000);
                }

                MediaRequest? mediaRequest = await _context.MediaRequests.FirstOrDefaultAsync(r => r.Locked == false && r.Done == false);

                if (mediaRequest == null)
                {
                    Thread.Sleep(_delay);
                    retryCount++;
                    continue;
                }

                mediaRequest.Locked = true;
                _context.MediaRequests.Update(mediaRequest);
                await _context.SaveChangesAsync();

                FileRequest? fileRequest = JsonSerializer.Deserialize<FileRequest>(mediaRequest.Data);

                if (fileRequest == null)
                {
                    mediaRequest.Done = true;
                    mediaRequest.Data = "Error: FileRequest is null";
                    _logger.LogError($"Error: FileRequest is null");
                    _context.MediaRequests.Update(mediaRequest);
                    await _context.SaveChangesAsync();
                    Thread.Sleep(_delay);
                    retryCount++;
                    continue;
                }

                switch (fileRequest.Operation)
                {
                    case "Save":
                        await SaveFileAsync(fileRequest);
                        break;
                    case "Get":
                        await GetFileAsync(fileRequest);
                        break;
                    case "Delete":
                        await DeleteFileAsync(fileRequest);
                        break;
                }

                mediaRequest.Done = true;
                mediaRequest.Locked = false;
                _context.MediaRequests.Update(mediaRequest);
                await _context.SaveChangesAsync();
                
                retryCount = 0;
                Thread.Sleep(_delay);
            }
        }

        private async Task SaveFileAsync(FileRequest request)
        {
            if (request.FileData == null)
            {
                throw new ArgumentNullException(nameof(request.FileData));
            }
            var fileData = Convert.FromBase64String(request.FileData);
            await _minioService.SaveFileAsync(request.FileName, fileData);
            _logger.LogInformation($"File {request.FileName} saved");
        }

        private async Task GetFileAsync(FileRequest request)
        {
            int lifeTime = 300;
            if (request.LifeTime != null)
            {
                lifeTime = request.LifeTime.Value;
            }
            var url = await _minioService.GeneratePresignedUrlAsync(request.FileName, lifeTime);
            await SendResponseAsync(request.CorrelationId, url);
            _logger.LogInformation($"Presigned URL for file {request.FileName} generated");
        }

        private async Task DeleteFileAsync(FileRequest request)
        {
            await _minioService.DeleteFileAsync(request.FileName);
            _logger.LogInformation($"File {request.FileName} deleted");
        }

        private async Task SendResponseAsync(string key, string response)
        {
            await _context.MediaResponses.AddAsync(new MediaResponse()
            {
                Key = key,
                Data = response,
                Done = false,
                Locked = false,
                Timestamp = DateTime.UtcNow.ToString("o")
            });

            await _context.SaveChangesAsync();
        }
    }
}
