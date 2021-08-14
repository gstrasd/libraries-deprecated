using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Dataflow;

namespace Library.Platform.Queuing
{
    public class QueueClientReadMessageException : Exception
    {
        public QueueClientReadMessageException(string rawMessage)
        {
            RawMessage = rawMessage;
        }

        public QueueClientReadMessageException(string rawMessage, string message) : base(message)
        {
            RawMessage = rawMessage;
        }

        public QueueClientReadMessageException(string rawMessage, string message, Exception innerException) : base(message, innerException)
        {
            RawMessage = rawMessage;
        }

        public string RawMessage { get; }
    }
}