using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Data.Consts;
using System.Text;

namespace RabbitMqLib.Client.Areas.Services
{
    public class RabbitMqService(ILogger<RabbitMqService> logger,
        IConfiguration configuration) : IRabbitMqSenderService, IRabbitMqReceiverService
    {
        private readonly IConnection _connection = GetConnection(configuration).Result;

        public async Task Send(string queue, string data, CancellationToken cancellationToken = default)
        {
            var channel = await CreateChannel(queue);

            await channel.BasicPublishAsync(string.Empty, queue, false, Encoding.UTF8.GetBytes(data), cancellationToken);

            await channel.CloseAsync(cancellationToken);
        }

        public async Task Receive(string queue, Func<object, string, Task> action,
            bool autoAck = true, bool requeue = false, ushort? prefetchCount = null,
            CancellationToken cancellationToken = default)
        {
            var channel = await CreateChannel(queue);

            if (prefetchCount.HasValue)
            {
                await channel.BasicQosAsync(0, prefetchCount.Value, false, cancellationToken);
            }

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {

                if (cancellationToken.IsCancellationRequested)
                    return;

                var body = ea.Body.ToArray();

                var message = Encoding.UTF8.GetString(body);

                try
                {
                    await action(model, message);

                    if (!autoAck)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during Action after receiving item from queue.");

                    if (!autoAck)
                    {
                        await channel.BasicRejectAsync(ea.DeliveryTag, requeue: requeue, cancellationToken);
                    }
                }
            };

            await channel.BasicConsumeAsync(queue,
                autoAck, consumer, cancellationToken);


            // Keep the method alive until cancellation is requested
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                logger.LogError("Cancelation Token canceled.");
            }

        }

        public async Task<Task?> Pull(string queue, Func<string, Task<object>> action,
            bool autoAck = true, bool requeue = false, CancellationToken cancellationToken = default)
        {
            var channel = await CreateChannel(queue);

            var ea = await channel.BasicGetAsync(queue, autoAck, cancellationToken);

            if (ea == null)
            {
                await channel.CloseAsync(cancellationToken);
                return null;
            }

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;

                await action(message);

                if (!autoAck)
                    await Acknowledge(queue, ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Action after receiving item from queue.");

                if (!autoAck)
                    await Reject(queue, ea.DeliveryTag, requeue);
            }
            finally
            {
                await channel.CloseAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
        //autoAck: true

        private async static Task<IConnection> GetConnection(IConfiguration configuration)
        {
            var credentials = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.ServerConfiguration.UsingCredentials];
            var hostName = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.ServerConfiguration.HostName] ??
                throw new NullReferenceException(RabbitMqConsts.ServerConfiguration.HostName);

            ConnectionFactory connectionFactory;

            if (!string.IsNullOrEmpty(credentials) && bool.Parse(credentials))
            {
                var userName = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.ServerConfiguration.UserName];
                var password = configuration[RabbitMqConsts.Section + ":" + RabbitMqConsts.ServerConfiguration.Password];

                if (userName == null || password == null)
                {
                    throw new NullReferenceException(RabbitMqConsts.ServerConfiguration.UserName + "or"
                        + RabbitMqConsts.ServerConfiguration.Password);
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
