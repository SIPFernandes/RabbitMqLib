namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqPublisherService
    {
        Task Send(string queue, string data, CancellationToken cancellationToken = default);
    }
}
