namespace Media_microservice.Entity
{
    public class MediaRequest
    {
        public long Id { get; set; }
        public string Key { get; set; }
        public string Timestamp { get; set; }
        public string Data { get; set; }
        public bool Locked { get; set; }
        public bool Done { get; set; }
    }
}
