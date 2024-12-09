namespace Media_microservice.Services
{
    public interface IKafkaService
    {
        Task StartListeningAsync();
    }
}
