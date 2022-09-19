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
        /// 发布
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="correlationId">相关ID</param>
        /// <returns>消息实体</returns>
        IMessage<T> Publish(T message, string correlationId = null);

        /// <summary>
        /// 回发
        /// </summary>
        /// <typeparam name="TReply">回发消息类型</typeparam>
        /// <param name="callback">回调函数</param>
        /// <param name="replyRoutingKey">回发路由键 (默认: r.routingKey)</param>
        IMessageProducer<T> ReplyTo<TReply>(Func<IMessage<TReply>, bool> callback, string replyRoutingKey = null);
    }
}

