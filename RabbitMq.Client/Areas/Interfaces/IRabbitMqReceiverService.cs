namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqReceiverService
    {
        Task Send(string queue, string data);
        Task Receive(string queue, Func<object, string, Task> action);
        Task<Thread?> Pull(string queue, Func<string, Task<object>> action,
            bool autoAck = true, bool requeue = false);
    }
}
