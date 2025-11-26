using RabbitMQ.Client;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IProcessQueueItemService
    {
        Task ProcessQueueItem(QueueItemModel queueItem, IReadOnlyBasicProperties basicProperties,
            CancellationToken cancellationToken = default);
    }
}
