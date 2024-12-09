namespace Media_microservice.Services
{
    public interface IMinioService
    {
        Task SaveFileAsync(string holder, string fileName, byte[] fileData);
        Task<byte[]> GetFileAsync(string holder, string fileName);
        Task<string> GeneratePresignedUrlAsync(string holder, string fileName, int expiryInSeconds);
    }
}
