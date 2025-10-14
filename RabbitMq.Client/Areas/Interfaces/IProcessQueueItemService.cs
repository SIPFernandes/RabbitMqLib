using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IProcessQueueItemService
    {
        Task ProcessQueueItem(QueueItemModel queueItem, CancellationToken cancellationToken = default);
    }
}
