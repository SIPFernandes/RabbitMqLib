using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Data.Consts;
using System.Text;

namespace RabbitMqLib.Client.Areas.Services
{
    public class RabbitMqService(IConfiguration configuration) : IRabbitMqSenderService, IRabbitMqReceiverService
    {
        private readonly IConnection _connection = GetConnection(configuration).Result;

        public async Task Send(string queue, string data)
        {
            var channel = await CreateChannel(queue);

            await channel.BasicPublishAsync(string.Empty, queue, false, Encoding.UTF8.GetBytes(data));

            await channel.CloseAsync();
        }

        public async Task Receive(string queue, Func<object, string, Task> action)
        {
            var channel = await CreateChannel(queue);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();

                var message = Encoding.UTF8.GetString(body);

                await action(model, message);
            };

            await channel.BasicConsumeAsync(queue,
                false, consumer);
        }

        public async Task<Thread?> Pull(string queue, Func<string, Task<object>> action,
            bool autoAck = true, bool requeue = false)
        {
            Thread? result = null;

            var channel = await CreateChannel(queue);

            var ea = await channel.BasicGetAsync(queue, autoAck);

            if (ea != null)
            {
                var body = ea.Body.ToArray();

                var message = Encoding.UTF8.GetString(body);

                result = new Thread(async () =>
                {
                    try
                    {
                        await action(message);

                        if (!autoAck)
                        {
                            await Acknowledge(queue, ea.DeliveryTag, false);
                        }
                    }
                    catch (Exception)
                    {
                        if (!autoAck)
                        {
                            await Reject(queue, ea.DeliveryTag, requeue);
                        }
                    }
                });

                result.Start();
            }

            await channel.CloseAsync();

            return result;
        }
        //autoAck: true

        private async static Task<IConnection> GetConnection(IConfiguration configuration)
        {
            var credentials = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.Configuration.UsingCredentials];
            var hostName = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.Configuration.HostName] ??
                throw new NullReferenceException(RabbitMqConsts.Configuration.HostName);

            ConnectionFactory connectionFactory;

            if (!string.IsNullOrEmpty(credentials) && bool.Parse(credentials))
            {
                var userName = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.Configuration.UserName];
                var password = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.Configuration.Password];

                if (userName == null || password == null)
                {
                    throw new NullReferenceException(RabbitMqConsts.Configuration.UserName + "or"
                        + RabbitMqConsts.Configuration.Password);
                }

                connectionFactory = new ConnectionFactory()
                {
                    HostName = hostName,
                    UserName = userName,
                    Password = password,
                };
            }
            else
            {
                connectionFactory = new ConnectionFactory()
                {
                    HostName = hostName,
                };
            }

            connectionFactory.RequestedChannelMax = 100;
            return await connectionFactory.CreateConnectionAsync();
        }

        private async Task<IChannel> CreateChannel(string queue)
        {
            var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue, true, false, false, null);

            return channel;
        }

        private async Task Acknowledge(string queue, ulong deliveryTag, bool multiple = false)
        {
            var newChanel = await CreateChannel(queue);

            await newChanel.BasicAckAsync(deliveryTag, multiple);
            
            await newChanel.CloseAsync();
        }

        private async Task Reject(string queue, ulong deliveryTag, bool requeue = false)
        {
            var newChanel = await CreateChannel(queue);
            
            await newChanel.BasicRejectAsync(deliveryTag, requeue);
            
            await newChanel.CloseAsync();
        }
    }
}
