using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Areas.Services;

namespace RabbitMqLib.Client
{
    public class Dependencies
    {
        public static void ConfigureServices(IConfiguration configuration, IServiceCollection services,
            bool queuePublisher = false, bool queueSubscriber = false)
        {
            services.AddSingleton<RabbitMqService>();

            if (queueSubscriber)
            {
                services.AddSingleton<IRabbitMqSubscriberService>(provider =>
                    provider.GetRequiredService<RabbitMqService>());

                services.AddSingleton<RabbitMqSubscriberClient>();
                services.AddHostedService(provider =>
                    provider.GetRequiredService<RabbitMqSubscriberClient>());

                //services.AddScoped<IRabbitMqClient, ProcessQueueItemService>();
            }

            if (queuePublisher)
            {
                services.AddSingleton<IRabbitMqPublisherService>(provider =>
                    provider.GetRequiredService<RabbitMqService>());

                services.AddSingleton<IRabbitMqPublisherClient, RabbitMqPublisherClient>();
            }
        }
    }
}
