using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Messaging
{
    public abstract class MessageProducer<TMessage>
    {
        protected ManagedChannel<TMessage> Channel { get; }

        protected MessageProducer(ManagedChannel<TMessage> channel)
        {
            Channel = channel;
        }

        public async Task<int> ExecuteAsync(CancellationToken token = default)
        {
            var processed = 0;

            // TODO: Pause when experiencing back pressure
            while (!token.IsCancellationRequested)
            {
                var messages = ProduceMessagesAsync(token);
                await foreach (var message in messages)
                {
                    // Stop writing to the channel if a cancellation has been requested.
                    if (token.IsCancellationRequested)
                    {
                        Channel.Writer.TryComplete();
                        break;
                    }

                    // Either channel writing has been completed or a cancellation has been requested.
                    // Either way, writing is no longer viable and execution should be abandoned.
                    if (!await Channel.Writer.WaitToWriteAsync(token)) break;

                    // Either the channel has been completed or the channel is full and we're in wait mode
                    if (!Channel.Writer.TryWrite(message)) break;

                    processed++;
                }
            }

            return processed;
        }

        protected abstract IAsyncEnumerable<TMessage> ProduceMessagesAsync(CancellationToken token);
    }
}
