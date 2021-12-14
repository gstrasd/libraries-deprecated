using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Dataflow;

namespace Library.Platform.Queuing
{
    public class QueueClientException : Exception
    {
        public QueueClientException(string message, string? queueName) : this(message, queueName, null)
        {
        }

        public QueueClientException(string message, string? queueName, Exception? innerException) : base(message, innerException)
        {
            QueueName = queueName;
        }

        public string? QueueName { get; }
    }
}