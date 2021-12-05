using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Dataflow;
using Library.Platform.Queuing;

namespace Library.Hosting
{
    public class QueueMessageWorker<T> : MessageWorker<T> where T : IMessage, new()
    {
        public QueueMessageWorker(QueueMessageProducer<T> producer, MessageConsumer<T> consumer, MessageWorkerConfiguration configuration = default) :
            base(producer, consumer, configuration)
        {
        }

        public QueueMessageWorker(IEnumerable<QueueMessageProducer<T>> producers, IEnumerable<MessageConsumer<T>> consumers, MessageWorkerConfiguration configuration = default) :
            base (producers, consumers, configuration)
        {
        }
    }
}