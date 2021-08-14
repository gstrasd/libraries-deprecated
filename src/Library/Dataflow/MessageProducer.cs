using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Library.Dataflow
{
    public abstract class MessageProducer<T> where T : IMessage     //TODO: Make this an IObservable<T>
    {
        private readonly ITargetBlock<T> _buffer;
        private readonly CancellationTokenSource _tokenSource;
        private bool _started;
        private Task _task;

        protected MessageProducer(ITargetBlock<T> buffer, CancellationToken token = default)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Completion.IsCompleted) throw new ArgumentException("Message buffer is already complete and cannot be written to.", nameof(buffer));

            _buffer = buffer;
            _tokenSource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token) : new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken token = default)
        {
            if (_buffer == null) return Task.CompletedTask;
            if (_started) throw new InvalidOperationException("Message producer already started.");
            if (_buffer.Completion.IsCompleted) throw new InvalidOperationException("Message buffer completed.");

            var execution = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token) : _tokenSource;

            _task = Task.Run(async () =>
            {
                while (!_buffer.Completion.IsCompleted && !_buffer.Completion.IsFaulted && !execution.IsCancellationRequested)
                {
                    try
                    {
                        var messages = ProduceMessagesAsync(execution.Token);
                        await foreach (var message in messages.WithCancellation(execution.Token))
                        {
                            await _buffer.SendAsync(message, execution.Token);
                        }
                    }
                    catch (Exception e)     // TODO: Need a way to recover from an error
                    {
                        throw;
                    }
                }

                _started = false;
            }, execution.Token);

            _started = true;
            return _task;
        }

        public async Task StopAsync(CancellationToken token = default)
        {
            if (!_started) return;

            try
            {
                _tokenSource.Cancel();
            }
            finally
            {
                await Task.WhenAny(_task, Task.Delay(Timeout.Infinite, token)).ConfigureAwait(false);
            }
        }

        protected abstract IAsyncEnumerable<T> ProduceMessagesAsync(CancellationToken token = default);
    }
}
