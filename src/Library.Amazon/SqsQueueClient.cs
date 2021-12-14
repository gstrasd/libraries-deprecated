using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.SQS;
using Amazon.SQS.Model;
using Library.Dataflow;
using Library.Platform.Queuing;

namespace Library.Amazon
{
    public class SqsQueueClient : IQueueClient, IDisposable
    {
        private static readonly Dictionary<Type, string> _queueTypes = new();
        private readonly IAmazonSQS _client;
        private readonly Dictionary<string, SqsQueueConfiguration> _configuration;
        private bool _disposed;

        public SqsQueueClient(IAmazonSQS client, List<SqsQueueConfiguration> configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _client = client;
            _configuration = configuration.ToDictionary(c => c.QueueName, c => c);
        }

        ~SqsQueueClient()
        {
            Dispose(false);
        }

        public async IAsyncEnumerable<string> ReadMessageAsync(string queueName, int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (queueName == null) throw new ArgumentNullException(nameof(queueName));

            if (!_configuration.TryGetValue(queueName, out var configuration))
            {
                throw new QueueClientException("Unable to locate queue url. Verify that this queue is properly configured.", queueName);
            }

            var request = new ReceiveMessageRequest
            {
                QueueUrl = configuration.QueueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = configuration.WaitTimeSeconds,
                VisibilityTimeout = configuration.VisibilityTimeout
            };

            ReceiveMessageResponse response;
            try
            {
                response = await _client.ReceiveMessageAsync(request, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new QueueClientException("An error occurred while reading a queue message.", queueName, e);
            }

            foreach (var message in response.Messages)
            {
                if (token.IsCancellationRequested) break;

                string decodedMessage;
                try
                {
                    var bytes = Convert.FromBase64String(message.Body);
                    decodedMessage = Encoding.UTF8.GetString(bytes);
                }
                catch (Exception e)
                {
                   throw new QueueClientException("An error occurred while decoding a queue message.", queueName, e);
                }

                yield return decodedMessage;

                var delete = new DeleteMessageRequest
                {
                    QueueUrl = queueName,
                    ReceiptHandle = message.ReceiptHandle,
                };

                try
                {
                    await _client.DeleteMessageAsync(delete, token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    throw new QueueClientException("An error occurred while deleting a queue message.", queueName, e);
                }
            }
        }

        public async IAsyncEnumerable<T> ReadMessageAsync<T>(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default) where T : IMessage
        {
            EnsureNotDisposed();

            var type = typeof(T);
            if (!_queueTypes.TryGetValue(type, out var queueName))
            {
                queueName = type.GetCustomAttribute<QueueNameAttribute>()?.QueueName;
                if (queueName == null) throw new QueueClientException("Unable to identify name of queue. Verify that it has a QueueName attribute.", null);

                _queueTypes.TryAdd(type, queueName);
            }

            if (!_configuration.TryGetValue(queueName!, out var configuration))
            {
                throw new QueueClientException("Unable to locate queue url. Verify that this queue is properly configured.", queueName!);
            }

            var request = new ReceiveMessageRequest
            {
                QueueUrl = configuration.QueueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = configuration.WaitTimeSeconds,
                VisibilityTimeout = configuration.VisibilityTimeout
            };

            ReceiveMessageResponse response;
            try
            {
                response = await _client.ReceiveMessageAsync(request, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new QueueClientException("An error occurred while reading a queue message.", queueName!, e);
            }

            foreach (var message in response.Messages)
            {
                if (token.IsCancellationRequested) break;

                T typedMessage;
                try
                {
                    var bytes = Convert.FromBase64String(message.Body);
                    string decodedMessage = Encoding.UTF8.GetString(bytes);
                    typedMessage = JsonSerializer.Deserialize<T>(decodedMessage)!;
                }
                catch (Exception e)
                {
                    throw new QueueClientException("An error occurred while deserializing a queue message.", queueName, e);
                }

                typedMessage.MessageId = message.MessageId;
                typedMessage.Receipt = message.ReceiptHandle;

                yield return typedMessage;
            }
        }

        public Task WriteMessageAsync(string queueName, string message, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (!_configuration.TryGetValue(queueName, out var configuration))
            {
                throw new QueueClientException("Unable to locate queue url. Verify that this queue is properly configured.", queueName!);
            }

            var bytes = Encoding.UTF8.GetBytes(message);
            var base64String = Convert.ToBase64String(bytes);

            return _client.SendMessageAsync(configuration.QueueUrl, base64String, token);
        }

        public Task WriteMessageAsync<T>(T message, CancellationToken token = default) where T : IMessage
        {
            EnsureNotDisposed();

            if (message == null) throw new ArgumentNullException(nameof(message));

            var type = typeof(T);
            if (!_queueTypes.TryGetValue(type, out var queueName))
            {
                queueName = type.GetCustomAttribute<QueueNameAttribute>()?.QueueName;
                if (queueName == null) throw new QueueClientException("Unable to identify name of queue. Verify that it has a QueueName attribute.", null);

                _queueTypes.TryAdd(type, queueName);
            }

            if (!_configuration.TryGetValue(queueName, out var configuration))
            {
                throw new QueueClientException("Unable to locate queue url. Verify that this queue is properly configured.", queueName!);
            }

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var base64String = Convert.ToBase64String(bytes);

            return _client.SendMessageAsync(configuration.QueueUrl, base64String, token);
        }

        public async Task DeleteMessageAsync(string queueName, string receipt, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (queueName == null) throw new ArgumentNullException(nameof(queueName));
            if (receipt == null) throw new ArgumentNullException(nameof(receipt));

            if (!_configuration.TryGetValue(queueName, out var configuration))
            {
                throw new QueueClientException("Unable to locate queue url. Verify that this queue is properly configured.", queueName!);
            }

            var delete = new DeleteMessageRequest
            {
                QueueUrl = queueName,
                ReceiptHandle = receipt,
            };

            try
            {
                await _client.DeleteMessageAsync(delete, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new QueueClientException("An error occurred while deleting a queue message.", queueName, e);
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
