using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Libraries.Messaging
{
    public abstract class MessageConsumer<TMessage>
    {
        protected ManagedChannel<TMessage> Channel { get; }

        protected MessageConsumer(ManagedChannel<TMessage> channel)
        {
            Channel = channel;
        }

        public async Task<int> ExecuteAsync(CancellationToken token = default)
        {
            var processed = 0;

            while (!token.IsCancellationRequested)
            {
                await Channel.Reader.WaitToReadAsync(token);
                var message = await Channel.Reader.ReadAsync(token);
                await ConsumeMessageAsync(message, token);
                processed++;
            }

            return processed;
        }

        protected abstract Task ConsumeMessageAsync(TMessage message, CancellationToken token);
    }
}