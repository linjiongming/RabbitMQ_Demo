using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息生产者
    /// </summary>
    public interface IMessageProducer : IDisposable
    {
        event EventHandler Disposed;
        event Action<string> PublishSuccess;
        event Action<string> PublishFailed;

        /// <summary>
        /// 客户端
        /// </summary>
        IMqClient Client { get; }

        /// <summary>
        /// 连接
        /// </summary>
        IConnection Connection { get; }

        /// <summary>
        /// 通道
        /// </summary>
        IModel Channel { get; }

        /// <summary>
        /// 绑定信息
        /// </summary>
        List<IRouteBinding> Bindings { get; }

        /// <summary>
        /// 绑定队列
        /// </summary>
        /// <param name="routingKey">路由键</param>
        /// <param name="exchangeMode">交换模式</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="ttl">最大存活时长</param>
        /// <returns></returns>
        IMessageProducer Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);

        /// <summary>
        /// 发布
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="correlationId">相关ID</param>
        /// <returns>消息实体</returns>
        IMessage Publish(string content, string correlationId = null);

        /// <summary>
        /// 回发
        /// </summary>
        /// <param name="replyRoutingKey">回发路由键</param>
        IMessageProducer ReplyTo(string replyRoutingKey);

        /// <summary>
        /// 等待确认
        /// </summary>
        /// <param name="timeout">最大等待时长</param>
        void WaitForConfirmsOrDie(TimeSpan timeout);
    }
}

