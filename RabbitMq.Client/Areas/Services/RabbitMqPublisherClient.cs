using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Data.Consts;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Services
{
    internal class RabbitMqPublisherClient : IRabbitMqPublisherClient
    {
        private readonly IRabbitMqPublisherService _rabbitMqService;
        private readonly ILogger<RabbitMqPublisherClient> _logger;
        private readonly Dictionary<string, string> _targetQueues;

        public RabbitMqPublisherClient(IRabbitMqPublisherService rabbitMqService,
            IConfiguration configuration, ILogger<RabbitMqPublisherClient> logger)
        {
            _rabbitMqService = rabbitMqService;
            _logger = logger;

            var section = configuration.GetSection(RabbitMqConsts.ClientConfiguration.TargetQueues);
            var children = section.GetChildren();

            if (!children.Any())
            {
                throw new InvalidOperationException(
                    $"Configuration section {nameof(RabbitMqConsts.ClientConfiguration.TargetQueues)} is empty or missing.");
            }

            _targetQueues = children.ToDictionary(
                x => x.Key,
                x => !string.IsNullOrWhiteSpace(x.Value) ? x.Value : throw new InvalidOperationException(
                    $"Value for key '{x.Key}' in {nameof(RabbitMqConsts.ClientConfiguration.TargetQueues)} is null or empty.")
                );
        }

        public async Task PublishQueueItem(IEnumerable<TargetQueueModel> targets, string data,
            BasicProperties? basicProperties = null)
        {
            foreach (var target in targets)
            {
                await PublishQueueItem(target, data, basicProperties);
            }
        }

        public async Task PublishQueueItem(TargetQueueModel target, string data,
            BasicProperties? basicProperties = null)
        {
            if (_targetQueues.TryGetValue(target.Type, out var queueName))
            {
                var queueItem = new QueueItemModel()
                {
                    Id = target.Id,
                    Data = data,
                    Type = target.Type,
                };

                await PushData(queueItem, queueName, basicProperties);
            }
            else
            {
                _logger.LogError("Queue Name for type {type} not found",
                    target.Type);
            }
        }

        private async Task PushData(QueueItemModel queueItem, string queueName,
            BasicProperties? basicProperties = null)
        {
            var jsonString = JsonConvert.SerializeObject(queueItem);

            await _rabbitMqService.Send(queueName, jsonString, basicProperties);
        }
    }
}
