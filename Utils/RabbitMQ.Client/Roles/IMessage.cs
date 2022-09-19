using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public interface IMessage
    {
        string CorrelationId { get; set; }
        ulong DeliveryTag { get; set; }
        string ReplyTo { get; set; }
        string Content { get; }
        [JsonIgnore]
        byte[] Body { get; }
        void Backup(string path);
        IMessage<T> Cast<T>();
    }
}
