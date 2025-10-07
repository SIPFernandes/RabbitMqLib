using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqSenderClient
    {
        Task PushDataToTarget(IEnumerable<TargetQueueModel> targets, string data);
    }
}
