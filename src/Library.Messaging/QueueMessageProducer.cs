using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Library.Queuing;

namespace Library.Messaging
{
    public class QueueMessageProducer<TMessage> : MessageProducer<TMessage>
    {
        private readonly IQueueClient _client;
        private readonly int _dequeueCount;

        public QueueMessageProducer(ManagedChannel<TMessage> channel, IQueueClient client, int dequeueCount) : base(channel)
        {
            _client = client;
            _dequeueCount = dequeueCount;
        }

        protected override async IAsyncEnumerable<TMessage> ProduceMessagesAsync([EnumeratorCancellation] CancellationToken token)
        {
            // TODO: If cancellation is requested (or an exception occurs) when messages are in channel, find a way to restore messages to source
            var messages = _client.ReadMessagesAsync(_dequeueCount, token);
            
            await foreach (var message in messages.WithCancellation(token))
            {
                yield return JsonSerializer.Deserialize<TMessage>(message);
            }
        }
    }
}
