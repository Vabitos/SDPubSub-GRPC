using RabbitMQ.Client;

namespace Server.RabbitMQ
{
    public class RabbitMQService
    {
        public IConnection Connection { get; }
        public IModel Channel { get; }

        public RabbitMQService(IConnection connection, IModel channel)
        {
            Connection = connection;
            Channel = channel;
        }
    }
}
