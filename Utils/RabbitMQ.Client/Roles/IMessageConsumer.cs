using RabbitMQ.Client.Events;
using System;
using System.ComponentModel;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息消费者
    /// </summary>
    public interface IMessageConsumer : IDisposable
    {
        event EventHandler Disposed;

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
        /// 订阅
        /// </summary>
        /// <param name="handler">消息处理方法</param>
        void Subscribe(Func<IMessage, object> handler);

        /// <summary>
        /// 订阅
        /// </summary>
        /// <param name="handler">消息处理方法</param>
        void Subscribe(Action<IMessage> handler);
    }
}

