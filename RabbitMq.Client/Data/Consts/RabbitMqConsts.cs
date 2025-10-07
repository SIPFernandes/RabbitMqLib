namespace RabbitMqLib.Client.Data.Consts
{
    public class RabbitMqConsts
    {
        public const string Section = "RabbitMQ";

        public class ServerConfiguration
        {
            public const string UsingCredentials = "UsingCredentials";
            public const string HostName = "HostName";
            public const string UserName = "UserName";
            public const string Password = "Password";
        }

        public class ClientConfiguration
        {
            public const string TargetQueues = "TargetQueues";
            public const string SourceQueues = "SourceQueues";
            public const string QueuePrefetchCount = "QueuePrefetchCount";
        }
    }
}
