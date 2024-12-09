namespace Media_microservice.Services
{
    public interface IMinioService
    {
        Task SaveFileAsync(string holder, string fileName, byte[] fileData);
        Task<string> GeneratePresignedUrlAsync(string holder, string fileName, int expiryInSeconds);
    }
}
