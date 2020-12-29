using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Messaging
{
    public abstract class MessageConsumer<TMessage>
    {
        protected MessageConsumer(ManagedChannel<TMessage> channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            Channel = channel;
        }

        protected ManagedChannel<TMessage> Channel { get; }

        public async Task<int> ExecuteAsync(CancellationToken token = default)
        {
            var processed = 0;

            while (!token.IsCancellationRequested)
            {
                await Channel.Reader.WaitToReadAsync(token);
                var message = await Channel.Reader.ReadAsync(token);
                await ProcessAsync(message, token);
                processed++;
            }

            return processed;
        }

        protected abstract Task ProcessAsync(TMessage message, CancellationToken token);
    }
}
