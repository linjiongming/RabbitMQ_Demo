using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class Message<T> : Message, IMessage<T>
    {
        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        protected T _value;

        [JsonIgnore]
        public T Value => Equals(_value, default(T)) ? (_value = JsonConvert.DeserializeObject<T>(_content)) : _value;

        public Message(T value, string correlationId = null)
        {
            _value = value;
            _content = JsonConvert.SerializeObject(Value, _jsonSerializerSettings);
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
        }

        public Message(byte[] body, ulong deliveryTag, IBasicProperties props) : base(body, deliveryTag, props)
        {
            _value = JsonConvert.DeserializeObject<T>(_content);
        }

        public Message(string content, ulong deliveryTag, string replyTo, string correlationId)
        {
            _content = content;
            DeliveryTag = deliveryTag;
            ReplyTo = replyTo;
            CorrelationId = correlationId;
        }

        public Message(BasicDeliverEventArgs args) : this(args.Body.ToArray(), args.DeliveryTag, args.BasicProperties)
        {
        }

        public Message(BasicGetResult result) : this(result.Body.ToArray(), result.DeliveryTag, result.BasicProperties)
        {
        }

    }
}
