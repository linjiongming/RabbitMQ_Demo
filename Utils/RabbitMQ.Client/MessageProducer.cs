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
    public class MessageProducer : IMessageProducer
    {
        public const int MaxRetryCount = 3;
        public const string FailedFolder = "publish_failed";

        private readonly List<IMessage> _msgList;
        private readonly Dictionary<ulong, int> _retryMap;
        private string _replyTo;

        public event EventHandler Disposed;

        public IMqClient Client { get; }
        public IConnection Connection { get; }
        public IModel Channel { get; }
        public string RoutingKey { get; }
        public ExchangeModes ExchangeMode { get; }
        public string QueueType { get; }
        public uint Ttl { get; }

        public string BackupPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FailedFolder, DateTime.Today.ToString("yyyy-MM-dd"), $"{RoutingKey}.txt");

        public MessageProducer() { }

        public MessageProducer(IMqClient client, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            _msgList = new List<IMessage>();
            _retryMap = new Dictionary<ulong, int>();

            Client = client;
            RoutingKey = routingKey;
            ExchangeMode = exchangeMode;
            QueueType = queueType;
            Ttl = ttl;
            Connection = Client.CreateConnection();
            Channel = Connection.CreateModel();
            Client.SetRoute(Channel, RoutingKey, exchangeMode, queueType, ttl);
            Channel.BasicAcks += Channel_BasicAcks;
            Channel.BasicNacks += Channel_BasicNacks;
            Channel.ConfirmSelect();
        }

        public IMessage Publish(string content, string correlationId = null)
        {
            IMessage message = new Message(content, correlationId);
            Publish(message);
            return message;
        }

        protected void Publish(IMessage message)
        {
            lock (_msgList)
            {
                _msgList.Add(message);
            }
            IBasicProperties props = Channel.CreateBasicProperties();
            {
                props.CorrelationId = message.CorrelationId;
                if (Client.Durable) props.Persistent = true;
                if (!string.IsNullOrEmpty(_replyTo)) props.ReplyTo = _replyTo;
            }
            message.DeliveryTag = Channel.NextPublishSeqNo;
            Channel.BasicPublish(Client.Exchange, RoutingKey, props, message.Body);
        }

        public IMessageProducer ReplyTo(Func<IMessage, object> callback, string replyTo = null)
        {
            IMessageConsumer consumer = null;
            Disposed += delegate { consumer?.Dispose(); };
            _replyTo = replyTo ?? $"r.{RoutingKey}";
            consumer = new MessageConsumer(Client, _replyTo, ExchangeMode, QueueType, Ttl);
            consumer.Subscribe(callback);
            return this;
        }

        /// <summary>
        /// 发布成功
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Channel_BasicAcks(object sender, Events.BasicAckEventArgs e)
        {
            lock (_msgList)
            {
                _msgList.RemoveAll(x => x.DeliveryTag == e.DeliveryTag);
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
            IMessage message = _msgList.FirstOrDefault(x => x.DeliveryTag == e.DeliveryTag);
            if (message != null)
            {
                if (!_retryMap.ContainsKey(e.DeliveryTag)) _retryMap[e.DeliveryTag] = 0;
                if (_retryMap[e.DeliveryTag] < MaxRetryCount) // 最大重试次数内 
                {
                    Trace.TraceInformation("Retring...");
                    Publish(message); // 重新发布
                    _retryMap[e.DeliveryTag]++;
                }
                else // 超过最大重试次数
                {
                    Trace.TraceInformation("Backup...");
                    message.Backup(BackupPath); // 备份到本地
                    lock (_msgList)
                    {
                        _msgList.Remove(message); // 从列表中删除
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Channel.Dispose();
                Connection.Dispose();
                Disposed?.Invoke(this, EventArgs.Empty);
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

