using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqClient
    {
        Task ConsumeQueueItem(QueueItemModel queueItem, CancellationToken cancellationToken = default);
    }
}
