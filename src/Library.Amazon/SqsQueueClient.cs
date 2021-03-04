using System;
using System.Buffers.Text;
using System.Collections.Generic;
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
using Library.Platform.Queuing;

namespace Library.Amazon
{
    public class SqsQueueClient : IQueueClient, IDisposable
    {
        private readonly IAmazonSQS _client;
        private readonly SqsQueueClientConfiguration _configuration;
        private bool _disposed;

        public SqsQueueClient(IAmazonSQS client, SqsQueueClientConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.QueueUrl == null) throw new ArgumentNullException(nameof(configuration.QueueUrl));
            if (configuration.QueueUrl.Trim().Length == 0) throw new ArgumentException("No queue name was provided.", nameof(configuration.QueueUrl));

            _client = client;
            _configuration = configuration;
            QueueName = _configuration.QueueUrl[(_configuration.QueueUrl.LastIndexOf("/") + 1)..];
        }

        ~SqsQueueClient()
        {
            Dispose(false);
        }

        public string QueueName { get; }

        public async Task WriteMessageAsync(string json, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (json == null) throw new ArgumentNullException(nameof(json));

            var bytes = Encoding.UTF8.GetBytes(json);
            var message = Convert.ToBase64String(bytes);

            await _client.SendMessageAsync(_configuration.QueueUrl, message, token);
        }

        public async Task WriteMessageAsync<TMessage>(TMessage message, CancellationToken token = default) where TMessage : IMessage
        {
            EnsureNotDisposed();

            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = JsonSerializer.Serialize(message);
            await WriteMessageAsync(json, token);
        }

        public async IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var request = new ReceiveMessageRequest
            {
                QueueUrl = _configuration.QueueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = _configuration.ReceiveWaitTimeSeconds,
                VisibilityTimeout = _configuration.ReceiveVisibilityTimeout
            };
            var response = await _client.ReceiveMessageAsync(request, token);

            foreach (var message in response.Messages)
            {
                if (token.IsCancellationRequested) break;
                
                var bytes = Convert.FromBase64String(message.Body);
                var json = Encoding.UTF8.GetString(bytes);

                yield return json;

                var delete = new DeleteMessageRequest
                {
                    QueueUrl = _configuration.QueueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                // If cancellation is requested, delete the message without awaiting a response
                if (token.IsCancellationRequested) _client.DeleteMessageAsync(delete, default).Start();
                else await _client.DeleteMessageAsync(delete, token);
            }
        }

        public async IAsyncEnumerable<TMessage> ReadMessagesAsync<TMessage>(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default) where TMessage : IMessage
        {
            EnsureNotDisposed();

            var messages = ReadMessagesAsync(messageCount, token);
            await foreach (var json in messages.WithCancellation(token))
            {
                var message = JsonSerializer.Deserialize<TMessage>(json);
                yield return message;
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

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _client?.Dispose();
            }

            _disposed = true;
        }
    }
}
