using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class RemoteProcedure : IRemoteProcedure
    {
        protected readonly List<IMessage> _resultList;
        protected readonly Dictionary<string, AutoResetEvent> _waitMap;

        public event EventHandler Disposed;

        public IMqClient Client { get; }

        public string RoutingKey { get; }
        public ExchangeModes ExchangeMode { get; }
        public string QueueType { get; }
        public uint Ttl { get; }
        private IMessageProducer Producer { get; set; }
        private IMessageConsumer Consumer { get; set; }

        public RemoteProcedure(IMqClient client, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            _resultList = new List<IMessage>();
            _waitMap = new Dictionary<string, AutoResetEvent>();

            Client = client;
            RoutingKey = routingKey;
            ExchangeMode = exchangeMode;
            QueueType = queueType;
            Ttl = ttl;
        }

        protected virtual bool Reply(IMessage result)
        {
            if (_waitMap.ContainsKey(result.CorrelationId))
            {
                lock (_resultList)
                {
                    _resultList.Add(result);
                }
                _waitMap[result.CorrelationId].Set();
                return true;
            }
            return false;
        }

        public virtual string Call(string content, int timeout = 30 * 1000)
        {
            if (Producer == null)
            {
                Producer = new MessageProducer(Client, RoutingKey, ExchangeMode, QueueType, Ttl);
                Disposed += delegate { Producer?.Dispose(); };
                Producer.ReplyTo(x => Reply(x));
            }
            IMessage message = Producer.Publish(content);
            lock (_waitMap)
            {
                _waitMap[message.CorrelationId] = new AutoResetEvent(false);
            }
            if (_waitMap[message.CorrelationId].WaitOne(timeout))
            {
                _waitMap[message.CorrelationId].Dispose();
                lock (_waitMap)
                {
                    _waitMap.Remove(message.CorrelationId);
                }
                IMessage result = _resultList.FirstOrDefault(x => x.CorrelationId == message.CorrelationId);
                if (result != null)
                {
                    lock (_resultList)
                    {
                        _resultList.Remove(result);
                    }
                    return result.Content;
                }
                Trace.TraceError($"Missing message[CorrelationId:{message.CorrelationId}]");
            }
            else
            {
                Trace.TraceError($"RemoteProcedure[{RoutingKey}] answer timeout.");
            }
            return null;
        }

        public async Task<string> CallAsync(string content, int timeout = 30 * 1000)
        {
            return await Task.Run(() => Call(content, timeout));
        }

        public void Answer(Func<IMessage, string> handler)
        {
            if (Consumer == null)
            {
                Consumer = new MessageConsumer(Client, RoutingKey, ExchangeMode, QueueType, Ttl);
                Disposed += delegate { Consumer?.Dispose(); };
            }
            Consumer.Subscribe(handler);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~RemoteProcedure()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }
}
