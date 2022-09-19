using System;
using System.ComponentModel;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息消费者
    /// </summary>
    public interface IMessageConsumer<T> : IMessageConsumer
    {
        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="handler">消息处理方法</param>
        void Subscribe(Action<IMessage<T>> handler);

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="handler">消息处理方法</param>
        void Subscribe<TReply>(Func<IMessage<T>, TReply> handler);
    }
}

