namespace RabbitMqLib.Client.Data.Models
{
    public class MessageModel
    {
        public required string PublisherService { get; set; }
        public required string Content { get; set; }
        public required string Action { get; set; }
        public required string TriggeredBy { get; set; }
    }
}
