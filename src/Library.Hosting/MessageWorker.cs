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
    public class MessageWorker<T> : BackgroundService, IBackgroundService where T : IMessage
    {
        private bool _paused;
        private CancellationTokenSource _tokenSource = new();
        private Task _executing;
        private readonly List<MessageProducer<T>> _producers;
        private readonly List<MessageConsumer<T>> _consumers;
        private readonly CancellationTokenSource _execution;

        public MessageWorker(MessageProducer<T> producer, string name) : this(new [] {producer}, Array.Empty<MessageConsumer<T>>(), name)
        {
        }

        public MessageWorker(IEnumerable<MessageProducer<T>> producers, string name) : this(producers, Array.Empty<MessageConsumer<T>>(), name)
        {
        }

        public MessageWorker(MessageConsumer<T> consumer, string name) : this(Array.Empty<MessageProducer<T>>(), new [] {consumer}, name)
        {
        }

        public MessageWorker(IEnumerable<MessageConsumer<T>> consumers, string name) : this(Array.Empty<MessageProducer<T>>(), consumers, name)
        {
        }

        public MessageWorker(MessageProducer<T> producer, MessageConsumer<T> consumer, string name) : this(new[] {producer}, new[] {consumer}, name)
        {
        }

        public MessageWorker(IEnumerable<MessageProducer<T>> producers, IEnumerable<MessageConsumer<T>> consumers, string name)
        {
            if (producers == null) throw new ArgumentNullException(nameof(producers));
            if (consumers == null) throw new ArgumentNullException(nameof(consumers));

            _producers = producers.ToList();
            _consumers = consumers.ToList();
            Name = name;
            _execution = new CancellationTokenSource();
        }

        public string Name { get; }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"Starting {Name}...");       // TODO: Change to _logger.Information()

            var source = CancellationTokenSource.CreateLinkedTokenSource(_execution.Token, stoppingToken);
            _executing = Task.CompletedTask;

            if (!_producers.Any() && !_consumers.Any()) return Task.CompletedTask;

            return Task.WhenAll(
                _producers.Select(p => p.StartAsync(source.Token))
                    .Concat(
                        _consumers.Select(c => c.StartAsync(source.Token))
                    ));
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine($"Stopping {Name}...");       // TODO: Change to _logger.Information()

            _execution.Cancel();

            if (!_producers.Any() && !_consumers.Any()) return Task.CompletedTask;

            return Task.WhenAll(
                _producers.Select(p => p.StopAsync(cancellationToken))
                    .Concat(
                        _consumers.Select(c => c.StopAsync(cancellationToken))
                    ));
        }
    }
}
