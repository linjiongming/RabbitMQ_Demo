using System;
using System.Collections.Generic;
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
        /// 绑定队列
        /// </summary>
        /// <param name="routingKey">路由键</param>
        /// <param name="exchangeMode">交换模式</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="ttl">最大存活时长</param>
        /// <returns></returns>
        IRemoteProcedure Bind(string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);

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
