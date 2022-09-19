using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 参数键
    /// </summary>
    public class ArgumentKeys
    {
        /// <summary>
        /// 队列类型
        /// </summary>
        public const string QueueType = "x-queue-type";

        /// <summary>
        /// 死信交换机
        /// </summary>
        public const string DeadLetterExchange = "x-dead-letter-exchange";

        /// <summary>
        /// 消息存活时长
        /// </summary>
        public const string TimeToLive = "x-message-ttl";
    }
}
