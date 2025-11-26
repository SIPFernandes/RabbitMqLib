using RabbitMQ.Client;

namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqPublisherService
    {
        Task Send(string queue, string data, BasicProperties? basicProperties = null,
            CancellationToken cancellationToken = default);
    }
}
