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
    public class Message : IMessage
    {
        protected string _content;
        protected byte[] _body;

        public ulong DeliveryTag { get; set; }
        public string ReplyTo { get; set; }
        public string CorrelationId { get; set; }
        public string Content => _content ?? (_content = Encoding.UTF8.GetString(Body));
        [JsonIgnore]
        public byte[] Body => _body ?? (_body = Encoding.UTF8.GetBytes(Content));

        public Message()
        {

        }

        public Message(string content, string correlationId = null)
        {
            _content = content;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString("N");
        }

        public Message(byte[] body, ulong deliveryTag, IBasicProperties props)
        {
            _body = body;
            _content = Encoding.UTF8.GetString(_body);
            DeliveryTag = deliveryTag;
            ReplyTo = props?.ReplyTo;
            CorrelationId = props?.CorrelationId;
        }

        public Message(BasicDeliverEventArgs args) : this(args.Body.ToArray(), args.DeliveryTag, args.BasicProperties)
        {
        }

        public Message(BasicGetResult result) : this(result.Body.ToArray(), result.DeliveryTag, result.BasicProperties)
        {
        }

        public void Backup(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            lock (string.Intern(path))
            {
                File.AppendAllLines(path, new string[] { JsonConvert.SerializeObject(this) });
            }
        }

        public IMessage<T> Cast<T>()
        {
            return new Message<T>(Content, DeliveryTag, ReplyTo, CorrelationId);
        }

        public override string ToString() => Content;
    }
}
