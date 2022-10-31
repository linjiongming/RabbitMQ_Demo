using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
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
        IMessageConsumer Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);

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

