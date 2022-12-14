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
        private readonly List<IMessage> _resultList;
        private readonly Dictionary<string, AutoResetEvent> _waitMap;

        public event EventHandler Disposed;

        public IMqClient Client { get; }

        private IMessageProducer Producer { get; set; }
        private IMessageConsumer Consumer { get; set; }
        private IMessageConsumer ReplyConsumer { get; set; }

        public RemoteProcedure(IMqClient client)
        {
            _resultList = new List<IMessage>();
            _waitMap = new Dictionary<string, AutoResetEvent>();

            Client = client;
            Producer = Client.CreateProducer();
            Consumer = client.CreateConsumer();
            ReplyConsumer = client.CreateConsumer();
        }

        public IRemoteProcedure Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            if (!Producer.Bindings.Any(x => x.RoutingKey == routingKey))
            {
                Producer.Bind(routingKey, exchangeMode, queueType, ttl);
                Consumer.Bind(routingKey, exchangeMode, queueType, ttl);
                string replyRoutingKey = $"r.{routingKey}";
                Producer.ReplyTo(replyRoutingKey);
                ReplyConsumer.Bind(replyRoutingKey, exchangeMode, queueType, ttl);
                ReplyConsumer.Subscribe(x => Reply(x));
            }
            return this;
        }

        private bool Reply(IMessage result)
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

        public string Call(string content, int timeout = 30 * 1000)
        {
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
                Trace.TraceError($"RemoteProcedure[{string.Join(",", Producer.Bindings.Select(x => x.RoutingKey))}] answer timeout.");
            }
            return null;
        }

        public async Task<string> CallAsync(string content, int timeout = 30 * 1000)
        {
            return await Task.Run(() => Call(content, timeout));
        }

        public void Answer(Func<IMessage, string> handler)
        {
            Consumer.Subscribe(handler);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Producer.Dispose();
                ReplyConsumer.Dispose();
                Consumer.Dispose();
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
