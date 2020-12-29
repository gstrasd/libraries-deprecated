using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Libraries.Http.Extensions;
using Libraries.Messaging;

namespace Libraries.Aws
{
    internal class AmazonSqsQueueClient : IQueueClient, IDisposable
    {
        private readonly IAmazonSQS _client;
        private readonly string _queueName;
        private readonly int _receiveWaitTimeSeconds;
        private readonly int _receiveVisibilityTimeout;
        private string _queueUrl;
        private bool _initialized;
        private bool _disposed;

        public AmazonSqsQueueClient(IAmazonSQS client, string queueName, int receiveWaitTimeSeconds = 5, int receiveVisibilityTimeout = 10)
        {
            _client = client;
            _queueName = queueName;
            _receiveWaitTimeSeconds = receiveWaitTimeSeconds;
            _receiveVisibilityTimeout = receiveVisibilityTimeout;
        }

        ~AmazonSqsQueueClient()
        {
            Dispose(false);
        }

        public string QueueName => _queueName;

        public async IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, CancellationToken token = default)
        {
            await EnsureInitializedAsync();
            
            var receive = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = _receiveWaitTimeSeconds,
                VisibilityTimeout = _receiveVisibilityTimeout
            };
            
            var response = await _client.ReceiveMessageAsync(receive, token);
            
            foreach (var message in response.Messages)
            {
                OnMessageReceived(message);
                if (token.IsCancellationRequested) break;

                yield return message.Body;

                var delete = new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                // If cancellation is requested, delete the message without awaiting a response
                if (token.IsCancellationRequested) _client.DeleteMessageAsync(delete).ContinueWith(_ => OnMessageDeleted(message)).Start();
                else
                {
                    await _client.DeleteMessageAsync(delete, token);
                    OnMessageDeleted(message);
                }
            }
        }

        public event Action<Message> MessageReceived;

        public event Action<Message> MessageDeleted;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) throw new ObjectDisposedException($"Cannot perform operations with a disposed {nameof(AmazonSqsQueueClient)}.");
        }

        private async Task EnsureInitializedAsync()
        {
            EnsureNotDisposed();
            
            if (_initialized) return;

            var listResponse = await _client.ListQueuesAsync(String.Empty);
            if (!listResponse.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code ({(int)listResponse.HttpStatusCode}) indicates queue client failed to obtain list of existing queues.");
            }

            _queueUrl = listResponse.QueueUrls.FirstOrDefault(url => url.EndsWith(_queueName, StringComparison.OrdinalIgnoreCase));
            if (_queueUrl != null)
            {
                _initialized = true;
                return;
            }

            var mutex = new Mutex(true, $"AWS:queue:{_queueName}");

            try
            {
                mutex.WaitOne(5000);

                if (_initialized) return;

                var createResponse = await _client.CreateQueueAsync(_queueName);
                if (!createResponse.HttpStatusCode.IsSuccess())
                {
                    throw new HttpRequestException($"Response status code ({(int) createResponse.HttpStatusCode}) indicates queue client failed to create \"{_queueName}\" queue.");
                }

                _queueUrl = createResponse.QueueUrl;

                // Give AWS time to guarantee queue creation
                await Task.Delay(1200);
                _initialized = true;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
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

        protected virtual void OnMessageReceived(Message message)
        {
            MessageReceived?.Invoke(message);
        }

        protected virtual void OnMessageDeleted(Message message)
        {
            MessageDeleted?.Invoke(message);
        }
    }
}
