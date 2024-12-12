namespace Media_microservice.Services
{
    public interface IMinioService
    {
        Task SaveFileAsync(string fileName, byte[] fileData);
        Task<string> GeneratePresignedUrlAsync(string fileName, int expiryInSeconds);
        Task DeleteFileAsync(string fileName);
    }
}
