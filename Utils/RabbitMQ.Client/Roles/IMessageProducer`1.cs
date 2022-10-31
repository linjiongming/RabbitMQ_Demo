using System;
using System.ComponentModel;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息生产者
    /// </summary>
    public interface IMessageProducer<T> : IMessageProducer
    {
        /// <summary>
        /// 绑定队列
        /// </summary>
        /// <param name="routingKey">路由键</param>
        /// <param name="exchangeMode">交换模式</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="ttl">最大存活时长</param>
        /// <returns></returns>
        new IMessageProducer<T> Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);


        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="correlationId">相关ID</param>
        /// <returns>消息实体</returns>
        IMessage<T> Publish(T message, string correlationId = null);
    }
}

