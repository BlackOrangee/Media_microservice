
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

        public async Task<string> GeneratePresignedUrlAsync(string holder, string fileName, int expiryInSeconds)
        {
            string objectName = $"{holder}/{fileName}";

            return await _minioClient.PresignedGetObjectAsync(new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithExpiry(expiryInSeconds));
        }

        public async Task<byte[]> GetFileAsync(string holder, string fileName)
        {
            string objectName = $"{holder}/{fileName}";
            var memoryStream = new MemoryStream();

            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream)));

            return memoryStream.ToArray();
        }

        public async Task SaveFileAsync(string holder, string fileName, byte[] fileData)
        {
            string objectName = $"{holder}/{fileName}";

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
