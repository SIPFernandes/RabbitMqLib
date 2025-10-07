namespace RabbitMqLib.Client.Data.Models
{
    public class QueueItemModel
    {
        public required Guid Id { get; set; }
        public required string Type { get; set; }
        public required string Data { get; set; }
    }
}
