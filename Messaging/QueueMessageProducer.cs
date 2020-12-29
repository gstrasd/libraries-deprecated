using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Libraries.Messaging
{
    public class QueueMessageProducer<TMessage> : MessageProducer<TMessage>
    {
        private readonly IQueueClient _client;
        private readonly int _count;

        public QueueMessageProducer(ManagedChannel<TMessage> channel, IQueueClient client, int count) : base(channel)
        {
            _client = client;
            _count = count;
        }

        protected override async IAsyncEnumerable<TMessage> ProduceMessagesAsync(CancellationToken token)
        {
            var messages = _client.ReadMessagesAsync(_count, token);

            await foreach (var message in messages)
            {
                yield return JsonSerializer.Deserialize<TMessage>(message);
            }
        }
    }
}
