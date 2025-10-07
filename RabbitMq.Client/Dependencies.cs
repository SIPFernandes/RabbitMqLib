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
            services.AddSingleton<RabbitMqClientService>();
            services.AddHostedService(provider =>
                provider.GetRequiredService<RabbitMqClientService>());

            services.AddSingleton<IRabbitMqReceiverService, RabbitMqService>();

            //services.AddScoped<IRabbitMqClient, ProcessQueueItemService>();
        }
    }
}
