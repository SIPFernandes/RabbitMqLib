using RabbitMQ.Client;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqQueueItemService
    {
        Task ProcessQueueItem(QueueItemModel queueItem, IReadOnlyBasicProperties basicProperties,
            CancellationToken cancellationToken = default);
    }
}
