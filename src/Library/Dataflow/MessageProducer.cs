using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Library.Dataflow
{
    public abstract class MessageProducer<T> where T : IMessage
    {
        private readonly ITargetBlock<T> _buffer;
        private bool _started;
        private Task? _task;

        protected MessageProducer(ITargetBlock<T> buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Completion.IsCompleted) throw new ArgumentException("Message buffer is already complete and cannot be written to.", nameof(buffer));

            _buffer = buffer;
        }

        public Task StartAsync(CancellationToken token = default)
        {
            if (_started) throw new InvalidOperationException("Message producer already started.");
            if (_buffer.Completion.IsCompleted) throw new InvalidOperationException("Message buffer completed.");

            _task = Task.Run(async () =>
            {
                while (!_buffer.Completion.IsCompleted && !_buffer.Completion.IsFaulted && !token.IsCancellationRequested)
                {
                    try
                    {
                        var messages = ProduceMessagesAsync(token);
                        await foreach (var message in messages.WithCancellation(token))
                        {
                            await _buffer.SendAsync(message, token);
                        }
                    }
                    catch (Exception e)     // TODO: Need a way to recover from an error
                    {
                        throw;
                    }
                }

                _started = false;
            }, token);

            _started = true;
            return _task;
        }

        public async Task StopAsync(CancellationToken token = default)
        {
            if (!_started) return;

            //try
            //{
            //    _tokenSource.Cancel();
            //}
            //finally
            //{
                // TODO: set this up so the cancellation token sent into the start method can be used to shut down these tasks
                await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, token)).ConfigureAwait(false);
            //}
        }

        protected abstract IAsyncEnumerable<T> ProduceMessagesAsync(CancellationToken token = default);
    }
}
