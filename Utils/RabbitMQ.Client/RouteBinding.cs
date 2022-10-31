using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabbitMQ.Client
{
    public class RouteBinding : IRouteBinding
    {
        public IModel Channel { get; }
        public string RoutingKey { get; private set; }
        public ExchangeModes ExchangeMode { get; }
        public string QueueType { get; }
        public uint TimeToLive { get; }
        public uint MessageCount { get; set; }
        protected CancellationTokenSource Cancellation { get; private set; }
        protected Task Holder { get; private set; }

        public RouteBinding(IModel channel, string routingKey, ExchangeModes exchangeMode = ExchangeModes.Normal, string queueType = null, uint timeToLive = 0)
        {
            Channel = channel;
            RoutingKey = routingKey;
            ExchangeMode = exchangeMode;
            QueueType = queueType;
            TimeToLive = timeToLive;
            LoadHolder();
        }

        protected void LoadHolder()
        {
            UnloadHolder();
            Cancellation = new CancellationTokenSource();
            Holder = Task.Run(delegate
            {
                while (Channel.IsOpen)
                {
                    Refresh();
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }, Cancellation.Token);
        }

        protected void UnloadHolder()
        {
            if (Holder == null) return;
            if (!Holder.Wait(TimeSpan.FromSeconds(2)))
            {
                Cancellation.Cancel();
            }
            Holder.Dispose();
            Holder = null;
        }

        protected void Refresh()
        {
            try
            {
                MessageCount = Channel.QueueDeclarePassive(RoutingKey).MessageCount;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        ~RouteBinding()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnloadHolder();
            }
        }
    }
}
