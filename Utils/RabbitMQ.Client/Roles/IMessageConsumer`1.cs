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
        /// 绑定队列
        /// </summary>
        /// <param name="routingKey">路由键</param>
        /// <param name="exchangeMode">交换模式</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="ttl">最大存活时长</param>
        /// <returns></returns>
        new IMessageConsumer<T> Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);

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

