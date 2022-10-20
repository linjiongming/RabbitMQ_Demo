using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageConsumer : IMessageConsumer
    {
        public const int MaxRetryCount = 3;
        public const string FailedFolder = "consume_failed";

        protected readonly IDictionary<string, object> _replyProducerMap;

        public event EventHandler Disposed;

        public IMqClient Client { get; }
        public IConnection Connection { get; }
        public IModel Channel { get; }
        public string RoutingKey { get; }
        public ExchangeModes ExchangeMode { get; }
        public string QueueType { get; }
        public uint Ttl { get; }

        public string BackupPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FailedFolder, DateTime.Today.ToString("yyyy-MM-dd"), $"{RoutingKey}.txt");
        public EventingBasicConsumer Consumer { get; }

        public MessageConsumer(IMqClient client, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            _replyProducerMap = new Dictionary<string, object>();

            Client = client;
            RoutingKey = routingKey;
            ExchangeMode = exchangeMode;
            QueueType = queueType;
            Ttl = ttl;
            Connection = Client.CreateConnection();
            Channel = Connection.CreateModel();
            Client.SetRoute(Channel, RoutingKey, exchangeMode, queueType, ttl);
            if (Client.Fairly) Channel.BasicQos(0, 1, false);
            Consumer = new EventingBasicConsumer(Channel);
        }

        public void Subscribe(Action<IMessage> handler)
        {
            Subscribe(x => { handler(x); return true; });
        }

        public void Subscribe(Func<IMessage, object> handler)
        {
            Consumer.Received += (sender, e) =>
            {
                bool success = false;
                object reply = null;
                IMessage message = null;
                try
                {
                    message = new Message(e);
                    reply = handler(message);
                    if (!string.IsNullOrEmpty(message.ReplyTo))
                    {
                        // 回发
                        IMessageProducer replyProducer = GetReplyProducer(message.ReplyTo);
                        replyProducer.Publish(reply.ToString(), message.CorrelationId);
                        success = true;
                    }
                    else
                    {
                        if (reply == null) return;
                        success = reply is bool result ? result : true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                if (success)
                {
                    // 消息确认 (销毁当前消息)
                    Channel.BasicAck(e.DeliveryTag, false);
                }
                else
                {
                    if (ExchangeMode == ExchangeModes.DLX)
                    {
                        long retryCount = GetRetryCount(e.BasicProperties.Headers); // 重试次数
                        if (retryCount < MaxRetryCount) // 最大重试次数内 转发给 Retry 交换机
                        {
                            // 定义下一次投递的间隔时间 ms
                            long interval = (retryCount + 1) * 10 * 1000 /*每多重试一次 增加10秒*/;
                            e.BasicProperties.Expiration = interval.ToString();

                            // 将消息投递给 Retry 交换机 (会自动增加 death 次数)
                            Trace.TraceInformation("Retring...");
                            Channel.BasicPublish(Client.ExchangeRetry, e.RoutingKey, e.BasicProperties, e.Body);

                            // 消息确认 (销毁当前消息)
                            Channel.BasicAck(e.DeliveryTag, false);
                        }
                        else // 超过最大重试次数
                        {
                            // 消息拒绝 投递到死信交换机
                            Trace.TraceInformation($"Deliver to DLX[CorrelationId:{e.BasicProperties.CorrelationId}]");
                            Channel.BasicNack(e.DeliveryTag, false, false);

                            // 备份到本地
                            Trace.TraceInformation("Backup...");
                            message.Backup(BackupPath);
                        }
                    }
                    else
                    {
                        // 消息拒绝
                        Trace.TraceInformation($"Nack message[CorrelationId:{e.BasicProperties.CorrelationId}]");
                        Channel.BasicNack(e.DeliveryTag, false, false);

                        // 备份到本地
                        Trace.TraceInformation("Backup...");
                        message.Backup(BackupPath);
                    }
                }
            };
            Channel.BasicConsume(RoutingKey, false, Consumer);
        }

        private IMessageProducer GetReplyProducer(string replyTo)
        {
            if (_replyProducerMap.TryGetValue(replyTo, out object producer))
            {
                return producer as IMessageProducer;
            }
            IMessageProducer replyProducer = new MessageProducer(Client, replyTo);
            Disposed += delegate { replyProducer?.Dispose(); };
            lock (_replyProducerMap)
            {
                _replyProducerMap[replyTo] = replyProducer;
            }
            return replyProducer;
        }

        private long GetRetryCount(IDictionary<string, object> headers)
        {
            IDictionary<string, object> death = null;
            if (headers != null && headers.ContainsKey("x-death"))
            {
                death = (headers["x-death"] as IList<object>).FirstOrDefault() as IDictionary<string, object>;
            }
            return (long)(death?["count"] ?? 0L);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Channel?.Dispose();
                Connection?.Dispose();
                Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~MessageConsumer()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }
}

