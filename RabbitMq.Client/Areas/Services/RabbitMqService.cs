using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Data.Consts;
using System.Text;

namespace RabbitMqLib.Client.Areas.Services
{
    public class RabbitMqService : IRabbitMqPublisherService, IRabbitMqSubscriberService
    {
        private readonly IConfigurationSection _rabbitMqSection;
        private readonly ILogger<RabbitMqService> _logger;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private IConnection? _connection;

        public RabbitMqService(ILogger<RabbitMqService> logger, IConfiguration configuration)
        {
            _logger = logger;

            var rabbitMqSection = configuration.GetSection(RabbitMqConsts.Section);
            if (!rabbitMqSection.Exists())
            {
                throw new NullReferenceException($"Configuration section '{RabbitMqConsts.Section}' is missing.");
            }

            _rabbitMqSection = rabbitMqSection;
        }

        public async Task Send(string queue, string data, BasicProperties? basicProperties = null,
            CancellationToken cancellationToken = default)
        {
            var channel = await CreateChannel(queue);

            await channel.BasicPublishAsync(string.Empty, queue, false,
                basicProperties ?? new(), Encoding.UTF8.GetBytes(data), cancellationToken);

            await channel.CloseAsync(cancellationToken);
        }

        public async Task Receive(string queue, Func<object, string, IReadOnlyBasicProperties, Task> action,
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
                    await action(model, message, ea.BasicProperties);

                    if (!autoAck)
                    {
                        await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Action after receiving item from queue.");

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
                _logger.LogError("Cancelation Token canceled.");
            }

        }

        public async Task<Task?> Pull(string queue, Func<string, IReadOnlyBasicProperties, Task<object>> action,
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

                await action(message, ea.BasicProperties);

                if (!autoAck)
                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Action after receiving item from queue.");

                if (!autoAck)
                    await channel.BasicRejectAsync(ea.DeliveryTag, requeue, cancellationToken);
            }
            finally
            {
                await channel.CloseAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }

        private async Task EnsureConnectedAsync()
        {
            await _connectionLock.WaitAsync();

            try
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection = await GetConnection();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to RabbitMQ");
                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task<IConnection> GetConnection()
        {
            var credentials = _rabbitMqSection[RabbitMqConsts.ServerConfiguration.UsingCredentials];
            var hostName = _rabbitMqSection[RabbitMqConsts.ServerConfiguration.HostName]
                ?? throw new NullReferenceException(RabbitMqConsts.ServerConfiguration.HostName);

            ConnectionFactory connectionFactory;

            if (!string.IsNullOrEmpty(credentials) && bool.Parse(credentials))
            {
                var userName = _rabbitMqSection[RabbitMqConsts.ServerConfiguration.UserName];
                var password = _rabbitMqSection[RabbitMqConsts.ServerConfiguration.Password];

                if (userName == null || password == null)
                {
                    throw new NullReferenceException($"{RabbitMqConsts.ServerConfiguration.UserName} or {RabbitMqConsts.ServerConfiguration.Password} is missing.");
                }

                connectionFactory = new ConnectionFactory
                {
                    HostName = hostName,
                    UserName = userName,
                    Password = password,
                };
            }
            else
            {
                connectionFactory = new ConnectionFactory
                {
                    HostName = hostName,
                };
            }

            connectionFactory.RequestedChannelMax = 100;
            return await connectionFactory.CreateConnectionAsync();
        }

        private async Task<IChannel> CreateChannel(string queue)
        {
            await EnsureConnectedAsync();

            if (_connection == null || !_connection.IsOpen)
                throw new InvalidOperationException("RabbitMQ connection is not available after EnsureConnectedAsync.");

            var channel = await _connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue, true, false, false, null);

            return channel;
        }
    }
}
