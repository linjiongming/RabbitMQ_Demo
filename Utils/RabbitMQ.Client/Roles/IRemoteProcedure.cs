using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 远程过程
    /// </summary>
    public interface IRemoteProcedure : IDisposable
    {
        event EventHandler Disposed;

        /// <summary>
        /// 客户端
        /// </summary>
        IMqClient Client { get; }

        /// <summary>
        /// 路由键
        /// </summary>
        string RoutingKey { get; }

        /// <summary>
        /// 队列类型
        /// </summary>
        string QueueType { get; }

        /// <summary>
        /// 消息存活时长
        /// </summary>
        uint Ttl { get; }

        /// <summary>
        /// 同步调用
        /// </summary>
        /// <param name="content">传入消息内容</param>
        /// <param name="timeout">超时 ms</param>
        /// <returns></returns>
        string Call(string content, int timeout = 30 * 1000);

        /// <summary>
        /// 异步调用
        /// </summary>
        /// <param name="content">传入消息内容</param>
        /// <param name="timeout">超时 ms</param>
        /// <returns></returns>
        Task<string> CallAsync(string content, int timeout = 30 * 1000);

        /// <summary>
        /// 响应
        /// </summary>
        /// <param name="handler">订阅方法</param>
        void Answer(Func<IMessage, string> handler);
    }
}
