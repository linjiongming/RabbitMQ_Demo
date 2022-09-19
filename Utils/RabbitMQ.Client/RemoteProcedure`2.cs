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
    public class RemoteProcedure<TSource, TResult> : RemoteProcedure, IRemoteProcedure<TSource, TResult>
    {
        private IMessageProducer<TSource> Producer { get; set; }
        private IMessageConsumer<TSource> Consumer { get; set; }

        public RemoteProcedure(IMqClient client, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
            : base(client, routingKey, exchangeMode, queueType, ttl)
        {
        }

        public TResult Call(TSource source, int timeout = 30 * 1000)
        {
            if (Producer == null)
            {
                Producer = new MessageProducer<TSource>(Client, RoutingKey, ExchangeMode, QueueType, Ttl);
                Disposed += delegate { Producer?.Dispose(); };
                Producer.ReplyTo<TResult>(Reply);
            }
            IMessage message = Producer.Publish(source);
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
                    return ((IMessage<TResult>)result).Value;
                }
                Trace.TraceError($"Missing message[CorrelationId:{message.CorrelationId}]");
            }
            else
            {
                Trace.TraceError($"RemoteProcedure[{RoutingKey}] answer timeout.");
            }
            return default(TResult);
        }

        public async Task<TResult> CallAsync(TSource source, int timeout = 30 * 1000)
        {
            return await Task.Run(() => Call(source, timeout));
        }

        public void Answer(Func<IMessage<TSource>, TResult> handler)
        {
            if (Consumer == null)
            {
                Consumer = new MessageConsumer<TSource>(Client, RoutingKey, ExchangeMode, QueueType, Ttl);
                Disposed += delegate { Consumer?.Dispose(); };
            }
            Consumer.Subscribe(handler);
        }
    }
}
