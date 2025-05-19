namespace RabbitMqLib.Client.Areas.Interfaces
{
    public interface IRabbitMqSenderService
    {
        Task Send(string queue, string data);
    }
}
