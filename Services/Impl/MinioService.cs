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

        public Task DeleteFileAsync(string holder, string fileName)
        {
            string objectName = $"{holder}/{fileName}";

            try
            {
                _logger.LogDebug($"Deleting file {fileName} with object name {objectName}");

                return _minioClient.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file {fileName} with object name {objectName}: {ex.Message}");
                throw new Exception($"Error deleting file {fileName} with object name {objectName}: {ex.Message}", ex);
            }
        }

        public async Task<string> GeneratePresignedUrlAsync(string holder, string fileName, int expiryInSeconds)
        {
            string objectName = $"{holder}/{fileName}";

            try
            {
                _logger.LogDebug($"Generating presigned URL for object {objectName}");

                return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithExpiry(expiryInSeconds));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating presigned URL for object {objectName}: {ex.Message}");
                throw new Exception($"Error generating presigned URL for object {objectName}: {ex.Message}", ex);
            }
        }

        public async Task SaveFileAsync(string holder, string fileName, byte[] fileData)
        {
            string objectName = $"{holder}/{fileName}";

            _logger.LogDebug($"Saving file {fileName} with object name {objectName}");

            using (var stream = new MemoryStream(fileData))
            {
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType("application/octet-stream"));
            }
        }
    }
}
