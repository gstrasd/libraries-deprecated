using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Library.Dataflow;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public class MessageWorker<T> : BackgroundService where T : IMessage
    {
        private bool _paused;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private Task _executing;
        private readonly List<MessageProducer<T>> _producers;
        private readonly List<MessageConsumer<T>> _consumers;
        private readonly MessageWorkerConfiguration _configuration;
        private readonly CancellationTokenSource _execution;

        public MessageWorker(MessageProducer<T> producer, MessageWorkerConfiguration configuration = default) : this(new [] {producer}, new MessageConsumer<T>[] { }, configuration)
        {
        }

        public MessageWorker(IEnumerable<MessageProducer<T>> producers, MessageWorkerConfiguration configuration = default) : this(producers, new MessageConsumer<T>[] { }, configuration)
        {
        }

        public MessageWorker(MessageConsumer<T> consumer, MessageWorkerConfiguration configuration = default) : this(new MessageProducer<T>[] { }, new [] {consumer}, configuration)
        {
        }

        public MessageWorker(IEnumerable<MessageConsumer<T>> consumers, MessageWorkerConfiguration configuration = default) : this(new MessageProducer<T>[] { }, consumers, configuration)
        {
        }

        public MessageWorker(MessageProducer<T> producer, MessageConsumer<T> consumer, MessageWorkerConfiguration configuration = default) : this(new[] {producer}, new[] {consumer}, configuration)
        {
        }

        public MessageWorker(IEnumerable<MessageProducer<T>> producers, IEnumerable<MessageConsumer<T>> consumers, MessageWorkerConfiguration configuration = default)
        {
            if (producers == null) throw new ArgumentNullException(nameof(producers));
            if (consumers == null) throw new ArgumentNullException(nameof(consumers));

            _producers = producers.ToList();
            _consumers = consumers.ToList();
            _configuration = configuration ?? MessageWorkerConfiguration.Default;
            _execution = new CancellationTokenSource();
        }

        public string Name { get; set; }    // TODO: this needs to be placed on an interface and passed into a constructor

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var source = CancellationTokenSource.CreateLinkedTokenSource(_execution.Token, stoppingToken);
            _executing = Task.CompletedTask;

            if (!_configuration.Enabled) return Task.Delay(Timeout.Infinite, source.Token);
            if (!_producers.Any() && !_consumers.Any()) return Task.CompletedTask;

            return Task.WhenAll(
                _producers.Select(p => p.StartAsync(source.Token))
                    .Concat(
                        _consumers.Select(c => c.StartAsync(source.Token))
                    ));
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _execution.Cancel();

            if (!_configuration.Enabled || !_producers.Any() && !_consumers.Any()) return Task.CompletedTask;

            return Task.WhenAll(
                _producers.Select(p => p.StopAsync(cancellationToken))
                    .Concat(
                        _consumers.Select(c => c.StopAsync(cancellationToken))
                    ));
        }

        //public Task Pause()
        //{
        //    if (_paused) return Task.CompletedTask;
        //    _paused = true;
        //    _tokenSource = new CancellationTokenSource();
        //    var task = Task.Delay(-1, _tokenSource.Token).ContinueWith(t => Task.CompletedTask);
        //}

        //public Task Resume()
        //{
        //    if (!_paused) return Task.CompletedTask;
        //    var source = new CancellationTokenSource();
            
        //}
    }
}
