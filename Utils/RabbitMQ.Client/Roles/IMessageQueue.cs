using System;
using System.Collections.Generic;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息队列
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageQueue : IEnumerable<IMessage>, IDisposable
    {
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
        /// 队列
        /// </summary>
        string Queue { get; }

        /// <summary>
        /// 路由绑定信息
        /// </summary>
        IRouteBinding Binding { get; }
    }
}
