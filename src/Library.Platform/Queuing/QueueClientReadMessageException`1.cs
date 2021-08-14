using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Platform.Queuing
{
    public class QueueClientReadMessageException<TMessage> : Exception
    {
        public QueueClientReadMessageException(TMessage queueMessage)
        {
            QueueMessage = queueMessage;
        }

        public QueueClientReadMessageException(TMessage queueMessage, string message) : base(message)
        {
            QueueMessage = queueMessage;
        }

        public QueueClientReadMessageException(TMessage queueMessage, string message, Exception innerException) : base(message, innerException)
        {
            QueueMessage = queueMessage;
        }

        public TMessage QueueMessage { get; }
    }
}