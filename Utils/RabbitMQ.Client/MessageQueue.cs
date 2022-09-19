using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageQueue : IMessageQueue
    {
        private MessageEnumerator _enumerator;

        public IMqClient Client { get; }
        public IConnection Connection { get; }
        public IModel Channel { get; }
        public string Queue { get; }

        public MessageQueue(IMqClient client, string queue, string queueType = null, uint ttl = 0)
        {
            Client = client;
            Connection = Client.CreateConnection();
            Channel = Connection.CreateModel();
            Queue = queue;
            Client.SetRoute(Channel, Queue, queueType: queueType, ttl: ttl);
            if (Client.Fairly) Channel.BasicQos(0, 1, false);
        }

        public IEnumerator<IMessage> GetEnumerator() => _enumerator ?? (_enumerator = new MessageEnumerator(Channel, Queue));

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            Channel?.Dispose();
            Connection?.Dispose();
        }

        public class MessageEnumerator : IEnumerator<IMessage>
        {
            public IModel Channel { get; }
            public string Queue { get; }
            public int Index { get; private set; }
            public BasicGetResult CurrentResult { get; private set; }
            public IList<IMessage> Messages { get; }
            public IMessage Current => Messages[Index];
            object IEnumerator.Current => Current;

            public MessageEnumerator(IModel channel, string queue)
            {
                Channel = channel;
                Queue = queue;
                Index = -1;
                CurrentResult = null;
                Messages = new List<IMessage>();
            }

            public bool MoveNext()
            {
                int nextIndex = Index + 1;
                if (nextIndex > Messages.Count - 1)
                {
                    if (CurrentResult != null)
                    {
                        Channel.BasicAck(CurrentResult.DeliveryTag, false);
                    }
                    CurrentResult = Channel.BasicGet(Queue, false);
                    if (CurrentResult != null)
                    {
                        IMessage message = GetMessage(CurrentResult);
                        Trace.TraceInformation($"Get message[{message.Content}]");
                        Messages.Add(message);
                    }
                }
                if (nextIndex < Messages.Count)
                {
                    Index = nextIndex;
                    return true;
                }
                return false;
            }

            protected virtual IMessage GetMessage(BasicGetResult result)
            {
                return new Message(result);
            }

            public void Reset()
            {
                Index = -1;
            }

            public void Dispose()
            {
                Reset();
            }
        }
    }
}
