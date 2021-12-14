using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Library.Dataflow;

namespace Library.Platform.Queuing
{
    public class QueueMessageProducer<T> : MessageProducer<T> where T : IMessage, new()
    {
        private readonly IQueueClient _client;
        private readonly int _messageCount;

        public QueueMessageProducer(IQueueClient client, ITargetBlock<T> buffer, int messageCount = 1) : base(buffer)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (messageCount <= 0) throw new ArgumentOutOfRangeException(nameof(messageCount), "Argument must be a positive, non-zero value.");

            _client = client;
            _messageCount = messageCount;
        }

        protected override IAsyncEnumerable<T> ProduceMessagesAsync(CancellationToken token = default)
        {
            return _client.ReadMessageAsync<T>(_messageCount, token);
        }
    }
}
