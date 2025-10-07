using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqLib.Client.Areas.Interfaces;
using RabbitMqLib.Client.Areas.Services;

namespace RabbitMqLib.Client
{
    public class Dependencies
    {
        public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton<RabbitMqService>();
            services.AddSingleton<IRabbitMqReceiverService>(provider =>
                provider.GetRequiredService<RabbitMqService>());
            services.AddSingleton<IRabbitMqSenderService>(provider =>
                provider.GetRequiredService<RabbitMqService>());

            services.AddSingleton<IRabbitMqSenderClient, RabbitMqSenderClient>();

            services.AddSingleton<RabbitMqReceiverClient>();
            services.AddHostedService(provider =>
                provider.GetRequiredService<RabbitMqReceiverClient>());

            //services.AddScoped<IRabbitMqClient, ProcessQueueItemService>();
        }
    }
}
