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

        public event EventHandler Disposed;

        private IDictionary<string, IMessageProducer> _replyProducerMap;

        public List<IRouteBinding> Bindings { get; private set; }
        public IMqClient Client { get; private set; }
        public IConnection Connection { get; private set; }
        public IModel Channel { get; private set; }

        public string GetBackupPath(string routingKey)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FailedFolder, DateTime.Today.ToString("yyyy-MM-dd"), $"{routingKey}.txt");
        }

        public EventingBasicConsumer Consumer { get; private set; }

        public MessageConsumer()
        {
        }

        public MessageConsumer(IMqClient client) : this()
        {
            Client = client;
            Initialize();
        }

        protected void Initialize()
        {
            _replyProducerMap = new Dictionary<string, IMessageProducer>();
            Bindings = new List<IRouteBinding>();
            Connection = Client.CreateConnection();
            Channel = Connection.CreateModel();
            if (Client.Fairly) Channel.BasicQos(0, 1, false);
        }

        public IMessageConsumer Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            if (!Bindings.Any(x => x.RoutingKey == routingKey))
            {
                IRouteBinding binding = Client.SetRoute(Channel, routingKey, exchangeMode, queueType, ttl);
                Bindings.Add(binding);
                Disposed += (sender, e) => binding.Dispose();
            }
            return this;
        }

        public void Subscribe(Action<IMessage> handler)
        {
            Subscribe(x => { handler(x); return true; });
        }

        public void Subscribe(Func<IMessage, object> handler)
        {
            ReceiveHandler = handler;
            Consumer = new EventingBasicConsumer(Channel);
            Consumer.Received += Consumer_Received;
            Consumer.Shutdown += Consumer_Shutdown;
            foreach (IRouteBinding bindInfo in Bindings)
            {
                Channel.BasicConsume(bindInfo.RoutingKey, false, Consumer);
            }
        }

        protected Func<IMessage, object> ReceiveHandler { get; private set; }

        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            bool success = false;
            object reply = null;
            IMessage message = null;
            IRouteBinding bindInfo = Bindings.FirstOrDefault(x => x.RoutingKey == e.RoutingKey);
            try
            {
                message = new Message(e);
                reply = ReceiveHandler(message);
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
                if (bindInfo.ExchangeMode == ExchangeModes.DLX)
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
                        message.Backup(GetBackupPath(e.RoutingKey));
                    }
                }
                else
                {
                    // 消息拒绝
                    Trace.TraceInformation($"Nack message[CorrelationId:{e.BasicProperties.CorrelationId}]");
                    Channel.BasicNack(e.DeliveryTag, false, false);

                    // 备份到本地
                    Trace.TraceInformation("Backup...");
                    message.Backup(GetBackupPath(e.RoutingKey));
                }
            }
        }

        private void Consumer_Shutdown(object sender, ShutdownEventArgs e)
        {
            Trace.TraceInformation($"{nameof(MessageConsumer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] is off. Cause {(e.Cause is Exception ex ? ex.Message : e.Cause.ToString())}");
            Dispose(true);
            Reboot();
        }

        private void Reboot()
        {
            Trace.TraceInformation($"{nameof(MessageConsumer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] rebooting...");
            while (!Client.TestConnection())
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            Trace.TraceInformation($"{nameof(MessageConsumer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] has rebooted.");
            IRouteBinding[] cloneBindings = new IRouteBinding[Bindings.Count];
            Bindings.CopyTo(cloneBindings);
            Initialize();
            foreach (var binding in cloneBindings)
            {
                Bind(binding.RoutingKey, binding.ExchangeMode, binding.QueueType, binding.TimeToLive);
            }
            if (ReceiveHandler != null)
            {
                Subscribe(ReceiveHandler);
            }
        }

        private IMessageProducer GetReplyProducer(string replyTo)
        {
            if (_replyProducerMap.TryGetValue(replyTo, out IMessageProducer producer))
            {
                return producer;
            }
            IMessageProducer replyProducer = new MessageProducer(Client).Bind(replyTo);
            Disposed += (sender, e) => replyProducer?.Dispose();
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
                death = (headers["x-death"] as List<object>).FirstOrDefault() as IDictionary<string, object>;
            }
            return (long)(death?["count"] ?? 0L);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Consumer != null)
                {
                    Consumer.Received -= Consumer_Received;
                    Consumer.Shutdown -= Consumer_Shutdown;
                    Consumer = null;
                }
                if (Channel != null)
                {
                    Channel.Dispose();
                    Channel = null;
                }
                if (Connection != null)
                {
                    Connection.Dispose();
                    Connection = null;
                }
                if (Disposed != null)
                {
                    Disposed.Invoke(this, EventArgs.Empty);
                    Disposed = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MessageConsumer()
        {
            Dispose(false);
        }
    }
}

