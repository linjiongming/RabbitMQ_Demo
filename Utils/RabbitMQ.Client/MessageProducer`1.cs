using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageProducer<T> : MessageProducer, IMessageProducer<T>
    {
        public MessageProducer(IMqClient client, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
            : base(client, routingKey, exchangeMode, queueType, ttl)
        {
        }

        public IMessage<T> Publish(T value, string correlationId = null)
        {
            IMessage<T> message = new Message<T>(value, correlationId);
            base.Publish(message);
            return message;
        }

        public IMessageProducer<T> ReplyTo<TReply>(Func<IMessage<TReply>, bool> callback, string replyTo = null)
        {
            base.ReplyTo(x => callback(x.Cast<TReply>()), replyTo);
            return this;
        }
    }
}

