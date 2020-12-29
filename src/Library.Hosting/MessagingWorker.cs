using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Messaging;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Library.Hosting
{
    public class MessagingWorker<TMessage> : BackgroundService
    {
        private readonly MessageProducer<TMessage> _producer;
        private readonly MessageConsumer<TMessage> _consumer;
        private readonly ILogger _logger;

        public MessagingWorker(MessageProducer<TMessage> producer, MessageConsumer<TMessage> consumer, ILogger logger)
        {
            _producer = producer;
            _consumer = consumer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.Information($"Starting the {GetType().Name} {typeof(TMessage).Name} background service...");
                await Task.WhenAll(
                    _producer.ExecuteAsync(stoppingToken),
                    _consumer.ExecuteAsync(stoppingToken));
            }
            catch (Exception e)
            {
                _logger.Error(e, $"An error occurred while executing the {GetType().Name} {typeof(TMessage).Name} background service.");
            }
        }
    }
}
