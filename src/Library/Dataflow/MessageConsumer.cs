using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Serilog.Context;

namespace Library.Dataflow
{
    public abstract class MessageConsumer<T> : IObservable<T> where T : IMessage
    {
        private readonly ObserverManager<T> _observerManager = new ObserverManager<T>();
        private readonly ISourceBlock<T> _buffer;
        private readonly AsyncLocal<Guid> _correlationId;
        private readonly CancellationTokenSource _tokenSource;
        private bool _started;
        private Task _task;

        protected MessageConsumer(ISourceBlock<T> buffer, AsyncLocal<Guid> correlationId, CancellationToken token = default)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Completion.IsCompleted) throw new ArgumentException("Message buffer is already complete and cannot be read from.", nameof(buffer));

            _buffer = buffer;
            _correlationId = correlationId;
            _tokenSource = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(token) : new CancellationTokenSource();
        }

        public IDisposable Subscribe(IObserver<T> observer) => _observerManager.Subscribe(observer);

        public Task StartAsync(CancellationToken token = default)
        {
            if (_buffer == null) return Task.CompletedTask;
            if (_started) throw new InvalidOperationException("Message consumer already started.");
            if (_buffer.Completion.IsCompleted) throw new InvalidOperationException("Message buffer completed.");

            var execution = token.CanBeCanceled ? CancellationTokenSource.CreateLinkedTokenSource(_tokenSource.Token, token) : _tokenSource;

            // TODO: Finish this to use dataflow instead of iterating buffer items
            // TODO: Figure out how IObservable could be used here
            //var executionOptions = new ExecutionDataflowBlockOptions
            //{
            //    MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded
            //};

            //var linkOptions = new DataflowLinkOptions
            //{
            //    PropagateCompletion = true
            //};

            //var action = _buffer.Action(async m =>
            //{
            //    if (execution.IsCancellationRequested) return;
            //    await ConsumeMessageAsync(m, execution.Token);

            //}, DataflowExtensions.RejectDefault<T>(), new ExecutionDataflowBlockOptions{}, new DataflowLinkOptions());


            _task = Task.Run(async () =>
            {
                while (!_buffer.Completion.IsCompleted && !_buffer.Completion.IsFaulted && !execution.IsCancellationRequested)
                {
                    if (await _buffer.OutputAvailableAsync(execution.Token))
                    {
                        var message = await _buffer.ReceiveAsync(token);

                        if (_correlationId != null) _correlationId.Value = message.CorrelationId;
                        await _observerManager.NotifyAsync(message);
                        await ConsumeMessageAsync(message, token);
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

        protected abstract Task ConsumeMessageAsync(T message, CancellationToken token = default);

        //private sealed class NullConsumer<T> : MessageConsumer<T> where T : IMessage
        //{
        //    internal NullConsumer() : base()
        //    {
        //    }

        //    protected override Task ConsumeMessageAsync(T message, CancellationToken token = default)
        //    {
        //        return Task.CompletedTask;
        //    }
        //}
    }
}
