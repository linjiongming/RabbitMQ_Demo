using System.Collections.Generic;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 消息队列客户端
    /// </summary>
    public interface IMqClient
    {
        /// <summary>
        /// 用户名
        /// </summary>
        string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        string Password { get; set; }

        /// <summary>
        /// 服务主机（集群则多个）
        /// </summary>
        IList<string> HostNames { get; set; }

        /// <summary>
        /// 持久化
        /// </summary>
        bool Durable { get; set; }

        /// <summary>
        /// 公平分发
        /// </summary>
        bool Fairly { get; set; }

        /// <summary>
        /// 交换类型
        /// </summary>
        string ExchangeType { get; set; }

        /// <summary>
        /// 接收正常消息的交换机
        /// </summary>
        string Exchange { get; }

        /// <summary>
        /// 接收重试消息的交换机
        /// </summary>
        string ExchangeRetry { get; }

        /// <summary>
        /// 接收失败消息的交换机
        /// </summary>
        string ExchangeFailed { get; }

        /// <summary>
        /// 连接工厂
        /// </summary>
        /// <returns></returns>
        IConnectionFactory Factory { get; }

        /// <summary>
        /// 创建连接
        /// </summary>
        /// <returns></returns>
        IConnection CreateConnection();

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <returns></returns>
        bool TestConnection();

        /// <summary>
        /// 创建生产者
        /// </summary>
        /// <returns></returns>
        IMessageProducer CreateProducer();

        /// <summary>
        /// 创建生产者
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <returns></returns>
        IMessageProducer<T> CreateProducer<T>();

        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <returns></returns>
        IMessageConsumer CreateConsumer();

        /// <summary>
        /// 创建消费者
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <returns></returns>
        IMessageConsumer<T> CreateConsumer<T>();

        /// <summary>
        /// 创建远程过程
        /// </summary>
        /// <returns></returns>
        IRemoteProcedure CreateRemoteProcedure();

        /// <summary>
        /// 创建远程过程
        /// </summary>
        /// <typeparam name="TSource">传入类型</typeparam>
        /// <typeparam name="TResult">传出类型</typeparam>
        /// <returns></returns>
        IRemoteProcedure<TSource, TResult> CreateRemoteProcedure<TSource, TResult>();

        /// <summary>
        /// 获取消息队列（当前全部消息枚举列表）
        /// </summary>
        /// <param name="queue">队列名</param>
        /// <returns></returns>
        IMessageQueue GetMessageQueue(string queue, string queueType = null, uint ttl = 0);

        /// <summary>
        /// 获取消息队列（当前全部消息枚举列表）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="queue">队列名</param>
        /// <returns></returns>
        IMessageQueue<T> GetMessageQueue<T>(string queue, string queueType = null, uint ttl = 0);

        /// <summary>
        /// 设置路由
        /// </summary>
        /// <param name="channel">通道</param>
        /// <param name="routingKey">路由键</param>
        /// <param name="exchangeMode">交换模式</param>
        /// <param name="queueType">队列类型</param>
        /// <param name="ttl">消息存活时长</param>
        /// <returns>路由绑定信息</returns>
        IRouteBinding SetRoute(IModel channel, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint ttl = 0);

    }
}
