using Newtonsoft.Json;
using RabbitMqLib.Client.Data.Models;

namespace RabbitMqLib.Client.Areas.Helpers
{
    public static class MessageHelper
    {
        public static string CreateMessage(string publisherName,
            object content, string action, string triggeredBy)
        {
            var msg = new MessageModel
            {
                PublisherService = publisherName,
                Content = JsonConvert.SerializeObject(content),
                Action = action,
                TriggeredBy = triggeredBy
            };

            return JsonConvert.SerializeObject(msg);
        }

        public static T? GetMessage<T>(string message)
        {
            return JsonConvert.DeserializeObject<T>(message);
        }
    }
}
