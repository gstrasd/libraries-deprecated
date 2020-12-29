using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime.SharedInterfaces;
using Amazon.SQS;
using Amazon.SQS.Model;
using Library.Amazon.Resources;
using Library.Http;
using Library.Queuing;

namespace Library.Amazon
{
    public class SqsQueueClient : IQueueClient, IDisposable
    {
        private readonly IAmazonSQS _client;
        private readonly SqsQueueClientConfiguration _configuration;
        private string _queueUrl;
        private bool _initialized;
        private bool _disposed;

        public SqsQueueClient(IAmazonSQS client, string queue) 
            : this(client, new SqsQueueClientConfiguration {QueueName = queue})
        {
        }

        public SqsQueueClient(IAmazonSQS client, SqsQueueClientConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.QueueName == null) throw new ArgumentNullException(nameof(configuration.QueueName));
            if (configuration.QueueName.Trim().Length == 0) throw new ArgumentException("No queue name was provided.", nameof(configuration.QueueName));

            _client = client;
            _configuration = configuration;
        }

        ~SqsQueueClient()
        {
            Dispose(false);
        }

        public async IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, [EnumeratorCancellation] CancellationToken token = default)
        {
            await EnsureInitializedAsync(token);

            var receive = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = messageCount,
                WaitTimeSeconds = _configuration.ReceiveWaitTimeSeconds,
                VisibilityTimeout = _configuration.ReceiveVisibilityTimeout
            };

            var response = await _client.ReceiveMessageAsync(receive, token);

            foreach (var message in response.Messages)
            {
                if (token.IsCancellationRequested) break;

                yield return message.Body;

                var delete = new DeleteMessageRequest
                {
                    QueueUrl = _queueUrl,
                    ReceiptHandle = message.ReceiptHandle
                };

                // If cancellation is requested, delete the message without awaiting a response
                if (token.IsCancellationRequested) _client.DeleteMessageAsync(delete, default).Start();
                else await _client.DeleteMessageAsync(delete, token);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task EnsureInitializedAsync(CancellationToken token = default)
        {
            if (_disposed) ExceptionHelper.ThrowDisposed(nameof(SqsQueueClient));
            if (_initialized) return;

            var manager = new SqsQueueManager(_client);
            var exists = await manager.QueueExistsAsync(_configuration.QueueName, token);
            if (exists)
            {
                _queueUrl = await manager.GetQueueUrlAsync(_configuration.QueueName, token);
                _initialized = true;
                return;
            }

            _queueUrl = await manager.CreateQueueAsync(_configuration.QueueName, token);
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
