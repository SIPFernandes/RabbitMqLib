using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqPublisherClient
    {
        Task PublishQueueItem(TargetQueueModel target, string data);
        Task PublishQueueItem(IEnumerable<TargetQueueModel> targets, string data);
    }
}
