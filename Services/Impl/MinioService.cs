using Minio;
using Minio.DataModel.Args;

namespace Media_microservice.Services.Impl
{
    public class MinioService : IMinioService
    {
        private readonly ILogger<MinioService> _logger;
        private readonly IMinioClient _minioClient;
        private readonly string _bucketName;

        public MinioService(ILogger<MinioService> logger, MinioClient minioClient, string bucketName)
        {
            _logger = logger;
            _minioClient = minioClient;
            _bucketName = bucketName;
        }

        public Task DeleteFileAsync(string fileName)
        {
            try
            {
                _logger.LogDebug($"Deleting file {fileName}");

                return _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file {fileName}: {ex.Message}");
                throw new Exception($"Error deleting file {fileName}: {ex.Message}", ex);
            }
        }

        public async Task<string> GeneratePresignedUrlAsync(string fileName, int expiryInSeconds)
        {
            try
            {
                _logger.LogDebug($"Generating presigned URL for object {fileName}");

                return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithExpiry(expiryInSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating presigned URL for object {fileName}: {ex.Message}");
                throw new Exception($"Error generating presigned URL for object {fileName}: {ex.Message}", ex);
            }
        }

        public async Task SaveFileAsync(string fileName, byte[] fileData)
        {

            _logger.LogDebug($"Saving file {fileName}");

            using (var stream = new MemoryStream(fileData))
            {
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/octet-stream"));
            }
        }
    }
}
