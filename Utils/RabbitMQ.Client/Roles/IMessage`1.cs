using Newtonsoft.Json;

namespace RabbitMQ.Client
{
    public interface IMessage<out T>: IMessage
    {
        T Value { get; }
    }
}
