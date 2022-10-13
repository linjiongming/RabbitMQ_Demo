using System;
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
        /// 路由键
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// 交换模式
        /// </summary>
        ExchangeModes ExchangeMode { get; }

        /// <summary>
        /// 队列类型
        /// </summary>
        string QueueType { get; }

        /// <summary>
        /// 消息存活时间
        /// </summary>
        uint Ttl { get; }

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
        /// <typeparam name="TReply">回发消息类型</typeparam>
        /// <param name="callback">回调函数</param>
        /// <param name="replyRoutingKey">回发路由键 (默认: r.routingKey)</param>
        IMessageProducer ReplyTo(Func<IMessage, object> callback, string replyRoutingKey = null);

        /// <summary>
        /// 等待确认
        /// </summary>
        /// <param name="timeout">最大等待时长</param>
        void WaitForConfirmsOrDie(TimeSpan timeout);
    }
}

