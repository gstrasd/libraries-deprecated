using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Amazon.SQS.Model;
using Library.Amazon.Resources;
using Library.Dataflow;
using Library.Platform.Queuing;

namespace Library.Amazon
{
    public class SqsQueueClient : IQueueClient, IObservable<string>, IObservable<IMessage>, IDisposable
    {
        private readonly ObserverManager<string> _messageObserverManager = new();
        private readonly ObserverManager<IMessage> _typedMessageObserverManager = new();
        private readonly IAmazonSQS _client;
        private readonly SqsQueueClientConfiguration _configuration;
        private bool _disposed;

        public SqsQueueClient(IAmazonSQS client, string queueUrl) : this(client, new SqsQueueClientConfiguration { QueueUrl = queueUrl })
        {
        }

        public SqsQueueClient(IAmazonSQS client, SqsQueueClientConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.QueueUrl == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.QueueUrl.Trim().Length == 0) throw new ArgumentException("No queue name was provided.", nameof(configuration));

            _client = client;
            _configuration = configuration;
            QueueName = _configuration.QueueUrl[(_configuration.QueueUrl.LastIndexOf("/", StringComparison.Ordinal) + 1)..];
        }

        public IDisposable Subscribe(IObserver<string> observer) => _messageObserverManager.Subscribe(observer);

        public IDisposable Subscribe(IObserver<IMessage> observer) => _typedMessageObserverManager.Subscribe(observer);

        ~SqsQueueClient()
        {
            Dispose(false);
        }

        public string QueueName { get; }

        public async Task WriteRawMessageAsync(string message, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (message == null) throw new ArgumentNullException(nameof(message));

            await _client.SendMessageAsync(_configuration.QueueUrl, message, token);
        }

        public async Task WriteMessageAsync(string message, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (message == null) throw new ArgumentNullException(nameof(message));

            var bytes = Encoding.UTF8.GetBytes(message);
            var base64String = Convert.ToBase64String(bytes);

            await WriteRawMessageAsync(base64String, token);
        }

        public async Task WriteMessageAsync<TMessage>(TMessage message, CancellationToken token = default) where TMessage : IMessage
        {
            EnsureNotDisposed();

            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64String = Convert.ToBase64String(bytes);

            await WriteRawMessageAsync(base64String, token);
        }

        public async IAsyncEnumerable<string> ReadRawMessageAsync(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var request = new ReceiveMessageRequest
            {
                QueueUrl = _configuration.QueueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = _configuration.WaitTimeSeconds,
                VisibilityTimeout = _configuration.VisibilityTimeout
            };

            var response = await _client.ReceiveMessageAsync(request, token);

            foreach (var message in response.Messages)
            {
                if (token.IsCancellationRequested) break;

                yield return message.Body;

                var delete = new DeleteMessageRequest
                {
                    QueueUrl = _configuration.QueueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                await _client.DeleteMessageAsync(delete, token);
            }
        }

        public async IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var messages = ReadRawMessageAsync(messageCount, token);

            await foreach (var message in messages.WithCancellation(token))
            {
                if (token.IsCancellationRequested) break;

                string decodedMessage;

                try
                {
                    var bytes = Convert.FromBase64String(message);
                    decodedMessage = Encoding.UTF8.GetString(bytes);
                }
                catch (Exception e)
                {
                    var error = new QueueClientReadMessageException(message, $"An error occurred while reading the body of a message read from the {QueueName} queue.", e);
                    await _messageObserverManager.NotifyErrorAsync(error);
                    continue;
                }

                await _messageObserverManager.NotifyAsync(decodedMessage);
                yield return decodedMessage;
            }
        }

        public async IAsyncEnumerable<TMessage> ReadMessagesAsync<TMessage>(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default) where TMessage : IMessage
        {
            EnsureNotDisposed();

            var messages = ReadRawMessageAsync(messageCount, token);

            await foreach (var message in messages.WithCancellation(token))
            {
                if (token.IsCancellationRequested) break;

                TMessage typedMessage;

                try
                {
                    var bytes = Convert.FromBase64String(message);
                    var json = Encoding.UTF8.GetString(bytes);
                    typedMessage = JsonSerializer.Deserialize<TMessage>(json);
                }
                catch (Exception e)
                {
                    var error = new QueueClientReadMessageException(message, $"An error occurred while deserializing the body of a message read from the {QueueName} queue.", e);
                    await _typedMessageObserverManager.NotifyErrorAsync(error);
                    continue;
                }

                await _typedMessageObserverManager.NotifyAsync(typedMessage);
                yield return typedMessage;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) ExceptionHelper.ThrowDisposed(nameof(SqsQueueClient));
        }

        private void Dispose(bool disposing)        // TODO: Implement DisposeAsync
        {
            if (_disposed) return;

            if (disposing)
            {
                _client?.Dispose();
            }

            Task.WhenAll(
                _messageObserverManager.NotifyCompleteAsync(),
                _typedMessageObserverManager.NotifyCompleteAsync()
            );

            _disposed = true;
        }
    }
}
