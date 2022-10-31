using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQ.Client
{
    public class MessageQueue<T> : MessageQueue, IMessageQueue<T>
    {
        private MessageEnumerator _enumerator;

        public MessageQueue(IMqClient client, string queue, string queueType = null, uint ttl = 0)
            : base(client, queue, queueType, ttl)
        {
        }

        IEnumerator<IMessage<T>> IEnumerable<IMessage<T>>.GetEnumerator() => _enumerator ?? (_enumerator = new MessageEnumerator(Channel, Queue));

        public new class MessageEnumerator : MessageQueue.MessageEnumerator, IEnumerator<IMessage<T>>
        {
            public MessageEnumerator(IModel channel, string queue)
                : base(channel, queue)
            {
            }

            IMessage<T> IEnumerator<IMessage<T>>.Current => (IMessage<T>)Current;

            protected override IMessage GetMessage(BasicGetResult result)
            {
                return new Message<T>(result);
            }
        }
    }
}
