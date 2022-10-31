using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageProducer : IMessageProducer
    {
        public const int MaxRetryCount = 3;
        public const string FailedFolder = "publish_failed";

        private List<IMessage> _publishTemp;
        private Dictionary<string, int> _retryMap;
        private string _replyTo;

        public event EventHandler Disposed;
        public event Action<string> PublishSuccess;
        public event Action<string> PublishFailed;

        public List<IRouteBinding> Bindings { get; private set; }
        public IMqClient Client { get; private set; }
        public IConnection Connection { get; private set; }
        public IModel Channel { get; private set; }

        public MessageProducer()
        {
        }

        public MessageProducer(IMqClient client) : this()
        {
            Client = client;
            Initialize();
        }

        protected void Initialize()
        {
            _publishTemp = new List<IMessage>();
            _retryMap = new Dictionary<string, int>();
            Bindings = new List<IRouteBinding>();
            Connection = Client.CreateConnection();
            Channel = Connection.CreateModel();
            Channel.ModelShutdown += Channel_ModelShutdown;
            Channel.BasicAcks += Channel_BasicAcks;
            Channel.BasicNacks += Channel_BasicNacks;
            Channel.ConfirmSelect();
        }

        private void Channel_ModelShutdown(object sender, ShutdownEventArgs e)
        {
            Trace.TraceInformation($"{nameof(MessageProducer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] is off. Cause {(e.Cause is Exception ex ? ex.Message : e.Cause.ToString())}");
            Dispose(true);
            Reboot();
        }

        public IMessageProducer Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            if (!Bindings.Any(x => x.RoutingKey == routingKey))
            {
                IRouteBinding binding = Client.SetRoute(Channel, routingKey, exchangeMode, queueType, ttl);
                Bindings.Add(binding);
                Disposed += (sender, e) => binding.Dispose();
            }
            return this;
        }

        public IMessage Publish(string content, string correlationId = null)
        {
            IMessage message = new Message(content, correlationId);
            Publish(message);
            return message;
        }

        protected void Publish(IMessage message)
        {
            lock (_publishTemp)
            {
                _publishTemp.Add(message);
            }
            IBasicProperties props = Channel.CreateBasicProperties();
            {
                props.CorrelationId = message.CorrelationId;
                if (Client.Durable) props.Persistent = true;
                if (!string.IsNullOrEmpty(_replyTo)) props.ReplyTo = _replyTo;
            }
            message.DeliveryTag = Channel.NextPublishSeqNo;
            foreach (IRouteBinding bindInfo in Bindings)
            {
                Channel.BasicPublish(Client.Exchange, bindInfo.RoutingKey, props, message.Body);
            }
        }

        public IMessageProducer ReplyTo(string replyTo)
        {
            _replyTo = replyTo;
            return this;
        }

        public void WaitForConfirmsOrDie(TimeSpan timeout)
        {
            Channel.WaitForConfirmsOrDie(timeout);
        }

        private void Reboot()
        {
            Trace.TraceInformation($"{nameof(MessageProducer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] rebooting...");
            while (!Client.TestConnection())
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
            Trace.TraceInformation($"{nameof(MessageProducer)}[{string.Join(",", Bindings.Select(x => x.RoutingKey))}] has rebooted.");
            IRouteBinding[] cloneBindings = new IRouteBinding[Bindings.Count];
            Bindings.CopyTo(cloneBindings);
            Initialize();
            foreach (var binding in cloneBindings)
            {
                Bind(binding.RoutingKey, binding.ExchangeMode, binding.QueueType, binding.TimeToLive);
            }
        }

        /// <summary>
        /// 发布成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Channel_BasicAcks(object sender, Events.BasicAckEventArgs e)
        {
            if (e.Multiple)
            {
                Acks(x => x.DeliveryTag <= e.DeliveryTag);
            }
            else
            {
                Acks(x => x.DeliveryTag == e.DeliveryTag);
            }
        }

        private void Acks(Func<IMessage, bool> predicate)
        {
            var msgs = _publishTemp.Where(predicate).ToList();
            foreach (var msg in msgs)
            {
                lock (_publishTemp)
                {
                    _publishTemp.Remove(msg); // 从列表中删除
                }
                PublishSuccess?.Invoke(msg.CorrelationId);
            }
        }

        /// <summary>
        /// 发布失败
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Channel_BasicNacks(object sender, Events.BasicNackEventArgs e)
        {
            Trace.TraceError($"Publish failed;\r\nDeliveryTag:{e.DeliveryTag};\r\nMultiple:{e.Multiple};");
            if (e.Multiple)
            {
                Nacks(x => x.DeliveryTag <= e.DeliveryTag);
            }
            else
            {
                Nacks(x => x.DeliveryTag == e.DeliveryTag);
            }
        }

        private void Nacks(Func<IMessage, bool> predicate)
        {
            var msgs = _publishTemp.Where(predicate).ToList();
            foreach (var msg in msgs)
            {
                lock (_publishTemp)
                {
                    _publishTemp.Remove(msg); // 从列表中删除
                }
                if (!_retryMap.ContainsKey(msg.CorrelationId)) _retryMap[msg.CorrelationId] = 0;
                if (_retryMap[msg.CorrelationId] < MaxRetryCount) // 最大重试次数内 
                {
                    Trace.TraceInformation("Retring...");
                    Publish(msg); // 重新发布
                    _retryMap[msg.CorrelationId]++;
                }
                else // 超过最大重试次数
                {
                    Trace.TraceInformation("Backup...");
                    foreach (IRouteBinding bindInfo in Bindings)
                    {
                        msg.Backup(GetBackupPath(bindInfo.RoutingKey)); // 备份到本地
                    }
                    PublishFailed?.Invoke(msg.CorrelationId);
                }
            }
        }

        private string GetBackupPath(string routingKey)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FailedFolder, DateTime.Today.ToString("yyyy-MM-dd"), $"{routingKey}.txt");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Channel != null)
                {
                    Channel.ModelShutdown -= Channel_ModelShutdown;
                    Channel.BasicAcks -= Channel_BasicAcks;
                    Channel.BasicNacks -= Channel_BasicNacks;
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
        }

        ~MessageProducer()
        {
            Dispose(false);
            GC.SuppressFinalize(this);
        }
    }
}

