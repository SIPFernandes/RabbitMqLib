namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqSubscriberService
    {
        Task Receive(string queue, Func<object, string, Task> action,
            bool autoAck = true, bool requeue = false, ushort? prefetchCount = null, 
            CancellationToken cancellationToken = default);
        Task<Task?> Pull(string queue, Func<string, Task<object>> action,
            bool autoAck = true, bool requeue = false, CancellationToken cancellationToken = default);
    }
}
