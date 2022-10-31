using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageConsumer<T> : MessageConsumer, IMessageConsumer<T>
    {
        public MessageConsumer(IMqClient client)
            : base(client)
        {
        }

        public new IMessageConsumer<T> Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            base.Bind(routingKey, exchangeMode, queueType, ttl);
            return this;
        }

        public void Subscribe(Action<IMessage<T>> handler)
        {
            Subscribe(x => { handler(x); return true; });
        }

        public void Subscribe<TReply>(Func<IMessage<T>, TReply> handler)
        {
            base.Subscribe(x => handler(x.Cast<T>()));
        }
    }
}

