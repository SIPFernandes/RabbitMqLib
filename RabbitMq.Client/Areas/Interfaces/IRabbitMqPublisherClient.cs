using RabbitMQ.Client;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqPublisherClient
    {
        Task PublishQueueItem(TargetQueueModel target, string data,
            BasicProperties? basicProperties = null);
        Task PublishQueueItem(IEnumerable<TargetQueueModel> targets, string data,
            BasicProperties? basicProperties = null);
    }
}
