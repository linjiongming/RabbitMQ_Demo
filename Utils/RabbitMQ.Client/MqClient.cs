using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MqClient : IMqClient
    {
        public const string DEFAULTUSERNAME = "timetrackpro";
        public const string DEFAULTPASSWORD = "tp2000";

        public static IMqClient FromConfig(string jsonFileName)
        {
            string jsonFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, jsonFileName);
            string json = File.ReadAllText(jsonFile);
            return JsonConvert.DeserializeObject<MqClient>(json);
        }

        private string _exchange;
        private string _exchangeRetry;
        private string _exchangeFailed;
        private IConnectionFactory _factory;

        public MqClient()
        {
            ExchangeType = ExchangeTypes.Direct;
            Durable = true;
            Fairly = true;
        }

        public MqClient(string userName, string password, params string[] hostNames) : this()
        {
            UserName = userName;
            Password = password;
            HostNames = hostNames;
        }

        public string UserName { get; set; }
        public string Password { get; set; }
        public IList<string> HostNames { get; set; }
        public bool Durable { get; set; }
        public bool Fairly { get; set; }
        public string ExchangeType { get; set; }

        public string Exchange => _exchange ?? (_exchange = $"x.{ExchangeType}");
        public string ExchangeRetry => _exchangeRetry ?? (_exchangeRetry = $"x.{ExchangeType}.retry");
        public string ExchangeFailed => _exchangeFailed ?? (_exchangeFailed = $"x.{ExchangeType}.failed");
        public IConnectionFactory Factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = new ConnectionFactory();
                    {
                        _factory.UserName = UserName.Equals(nameof(DEFAULTUSERNAME), StringComparison.OrdinalIgnoreCase) ? DEFAULTUSERNAME : UserName;
                        _factory.Password = Password.Equals(nameof(DEFAULTPASSWORD), StringComparison.OrdinalIgnoreCase) ? DEFAULTPASSWORD : Password;
                        _factory.ClientProperties.Add("connection_name", "RabbitMQ.Client"); // 为兼容3.5.7显示客户端名称问题
                        _factory.ClientProperties.Add("tag", "RabbitMQ.Client");             // 为解决5.2.0版本不显示客户端名称问题
                    }
                }
                return _factory;
            }
        }

        public IConnection CreateConnection()
        {
            IConnection connection = Factory.CreateConnection(HostNames);
            return connection;
        }

        public bool TestConnection()
        {
            try
            {
                using (CreateConnection()) { }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IMessageProducer CreateProducer()
        {
            return new MessageProducer(this);
        }

        public IMessageProducer<T> CreateProducer<T>()
        {
            return new MessageProducer<T>(this);
        }

        public IMessageConsumer CreateConsumer()
        {
            return new MessageConsumer(this);
        }

        public IMessageConsumer<T> CreateConsumer<T>()
        {
            return new MessageConsumer<T>(this);
        }

        public IRemoteProcedure CreateRemoteProcedure()
        {
            return new RemoteProcedure(this);
        }

        public IRemoteProcedure<TSource, TResult> CreateRemoteProcedure<TSource, TResult>()
        {
            return new RemoteProcedure<TSource, TResult>(this);
        }

        public IMessageQueue GetMessageQueue(string queue, string queueType = null, uint ttl = 0)
        {
            return new MessageQueue(this, queue, queueType, ttl);
        }

        public IMessageQueue<T> GetMessageQueue<T>(string queue, string queueType = null, uint ttl = 0)
        {
            return new MessageQueue<T>(this, queue, queueType, ttl);
        }

        public IRouteBinding SetRoute(IModel channel, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0)
        {
            string queue = $"{routingKey}";

            IDictionary<string, object> args = new Dictionary<string, object>();
            {
                if (!string.IsNullOrWhiteSpace(queueType))
                    args[ArgumentKeys.QueueType] = queueType;
                if (ttl > 0)
                    args[ArgumentKeys.TimeToLive] = ttl;
                if (exchangeMode == ExchangeModes.DLX)
                    args[ArgumentKeys.DeadLetterExchange] = ExchangeFailed;         // 指定死信交换机，用于将 Noraml 队列中失败的消息投递给 Failed 交换机
            }

            channel.ExchangeDeclare(Exchange, ExchangeType, Durable, false, null);
            channel.QueueDeclare(queue, Durable, false, false, args);
            channel.QueueBind(queue, Exchange, routingKey);

            if (exchangeMode == ExchangeModes.DLX)
            {
                string queueRetry = $"{routingKey}.retry";
                string queueFailed = $"{routingKey}.failed";

                IDictionary<string, object> retryArgs = new Dictionary<string, object>();
                {
                    if (!string.IsNullOrWhiteSpace(queueType))
                        retryArgs[ArgumentKeys.QueueType] = queueType;
                    retryArgs[ArgumentKeys.DeadLetterExchange] = Exchange;              // 指定死信交换机，用于将 Retry 队列中超时的消息投递给 Noraml 交换机
                    retryArgs[ArgumentKeys.TimeToLive] = ttl > 0 ? ttl : (6 * 1000);    // 定义 queueRetry 的消息最大停留时间 (原理是：等消息超时后由 broker 自动投递给当前绑定的死信交换机)
                                                                                        // 定义最大停留时间为防止一些 待重新投递 的消息、没有定义重试时间而导致内存溢出
                }

                IDictionary<string, object> failedArgs = new Dictionary<string, object>();
                {
                    if (!string.IsNullOrWhiteSpace(queueType))
                        failedArgs[ArgumentKeys.QueueType] = queueType;
                }

                channel.ExchangeDeclare(ExchangeRetry, ExchangeType, Durable, false, null);
                channel.QueueDeclare(queueRetry, Durable, false, false, retryArgs);
                channel.QueueBind(queueRetry, ExchangeRetry, routingKey);

                channel.ExchangeDeclare(ExchangeFailed, ExchangeType, Durable, false, null);
                channel.QueueDeclare(queueFailed, Durable, false, false, failedArgs);
                channel.QueueBind(queueFailed, ExchangeFailed, routingKey);
            }

            return new RouteBinding(channel, routingKey, exchangeMode, queueType, ttl);
        }

    }
}
