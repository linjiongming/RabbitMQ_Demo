namespace RabbitMQ.Client
{
    /// <summary>
    /// 队列类型
    /// </summary>
    public class QueueTypes
    {
        /// <summary>
        /// 默认队列
        /// </summary>
        public const string Classic = "classic";

        /// <summary>
        /// 仲裁队列
        /// <para>仲裁队列是镜像队列(又称为HA队列)的替代方案，该队列把数据安全作为首要目标，在3.8.0版本可以使用。声明仲裁队列和声明普通队列方法一样，只需要把x-queue-type设置为quorum即可。</para>
        /// </summary>
        public const string Quorum = "quorum";
    }
}
