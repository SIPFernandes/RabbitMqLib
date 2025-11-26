using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMqLib.Client.Areas.Helpers;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Data.Consts;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Services
{
    public class RabbitMqSubscriberClient : IHostedService
    {
        private readonly IRabbitMqSubscriberService _rabbitMqService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMqSubscriberClient> _logger;
        private readonly Dictionary<string, HashSet<string>> _sourceQueues;
        private readonly List<Task> _runningTasks = [];
        private volatile CancellationTokenSource? _cts;
        private readonly ushort _queuePrefetchCount = 0;

        public RabbitMqSubscriberClient(IRabbitMqSubscriberService rabbitMqService,
            IServiceProvider serviceProvider, IConfiguration configuration,
            ILogger<RabbitMqSubscriberClient> logger)
        {
            _rabbitMqService = rabbitMqService;
            _serviceProvider = serviceProvider;
            _logger = logger;

            var queuePrefetchCount = configuration[RabbitMqConsts.ClientConfiguration.QueuePrefetchCount];

            _ = ushort.TryParse(queuePrefetchCount, out _queuePrefetchCount);

            var section = configuration.GetSection(RabbitMqConsts.ClientConfiguration.SourceQueues);
            var rawDictionary = new Dictionary<string, string[]>();

            section.Bind(rawDictionary);

            if (rawDictionary.Count <= 0)
            {
                throw new InvalidOperationException(
                    $"Configuration section {nameof(RabbitMqConsts.ClientConfiguration.SourceQueues)} is empty or missing.");
            }

            _sourceQueues = rawDictionary.ToDictionary(
                x => x.Key,
                x => x.Value?.Where(v => !string.IsNullOrWhiteSpace(v)).ToHashSet()
                    is { Count: > 0 } validSet ? validSet : throw new InvalidOperationException(
                    $"Value for key '{x.Key}' in {nameof(RabbitMqConsts.ClientConfiguration.SourceQueues)}" +
                    $" is null, empty, or contains only invalid strings."));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            foreach (var queueName in _sourceQueues.Keys)
            {
                var task = Task.Run(() => StartResilientListener(queueName, _cts.Token), _cts.Token);

                _runningTasks.Add(task);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();

            try
            {
                await Task.WhenAll(_runningTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for RabbitMQ listener tasks to shut down.");
            }

            _runningTasks.Clear();
            _cts = null;
        }

        private async Task StartResilientListener(string queueName, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _rabbitMqService.Receive(queueName, OnQueueItemReceive, false, false, _queuePrefetchCount, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Listener for queue '{queueName}' failed. Retrying in 5 seconds...", queueName);

                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                }
            }
        }

        private async Task OnQueueItemReceive(object queueData, string message,
            IReadOnlyBasicProperties basicProperties)
        {
            try
            {
                if (queueData is not AsyncDefaultBasicConsumer queue)
                {
                    _logger.LogError("Invalid queue data received.");

                    return;
                }

                var queueName = queue.Channel.CurrentQueue;

                if (string.IsNullOrEmpty(queueName) ||
                    !_sourceQueues.TryGetValue(queueName, out var queueItemTypes))
                {
                    _logger.LogError("Queue Name is missing from received queue data.");

                    return;
                }

                var queueItem = MessageHelper.GetMessage<QueueItemModel>(message);

                if (queueItem == null)
                {
                    _logger.LogError("Invalid queue message received, expected format {format}.",
                            nameof(QueueItemModel));

                    return;
                }

                if (!queueItemTypes.Contains(queueItem.Type))
                {
                    _logger.LogError("Invalid queue item type {type} for queue named {queueName}.",
                            queueItem.Type, queueName);

                    return;
                }

                await ProcessQueueItem(queueItem, basicProperties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Queue Item.");
            }
        }

        private async Task ProcessQueueItem(QueueItemModel queueItem,
            IReadOnlyBasicProperties basicProperties)
        {
            var cancelationToken = _cts?.Token ?? CancellationToken.None;

            using var scope = _serviceProvider.CreateScope();

            var processQueueItemService = scope.ServiceProvider.GetRequiredService<IProcessQueueItemService>();

            await processQueueItemService.ProcessQueueItem(queueItem, basicProperties, cancelationToken);
        }
    }
}
