namespace RabbitMQ.Client
{
    /// <summary>
    /// 交换机类型
    /// </summary>
    public class ExchangeTypes
    {
        /// <summary>
        /// 将消息路由到那些binding key与routing key完全匹配的队列中
        /// </summary>
        public const string Direct = "direct";
        /// <summary>
        /// 将消息路由到所有绑定的队列中
        /// </summary>
        public const string Fanout = "fanout";
        /// <summary>
        /// 将消息路由到具有相同header arguments的队列中
        /// </summary>
        public const string Headers = "headers";
        /// <summary>
        /// 将消息路由到binding key与routing key模式匹配的队列中 (通配符: '*', '#', 其中'*'表示匹配一个单词, '#'则表示匹配没有或者多个单词)
        /// </summary>
        public const string Topic = "topic";
    }
}