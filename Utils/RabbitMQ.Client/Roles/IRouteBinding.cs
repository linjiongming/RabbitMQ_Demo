using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public interface IRouteBinding : IDisposable
    {
        IModel Channel { get; }
        string RoutingKey { get; }
        ExchangeModes ExchangeMode { get; }
        string QueueType { get; }
        uint TimeToLive { get; }
        uint MessageCount { get; set; }
    }
}
