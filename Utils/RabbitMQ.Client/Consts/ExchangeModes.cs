using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    /// <summary>
    /// 交换模式
    /// </summary>
    public enum ExchangeModes
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 死信交换模式
        /// </summary>
        DLX = 1,
    }
}