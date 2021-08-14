using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Amazon.SQS;
using Amazon.SQS.Model;
using Library.Amazon.Resources;
using Library.Platform.Queuing;

namespace Library.Amazon
{
    // TODO: Update to follow coding patterns in S3StorageManager class
    public class SqsQueueManager : IQueueManager
    {
        private static readonly SemaphoreSlim _createSemaphore = new SemaphoreSlim(1, 1);
        private readonly IAmazonSQS _client;

        public SqsQueueManager(IAmazonSQS client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }
        
        public async Task<bool> QueueExistsAsync(string queue, CancellationToken token = default)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var response = await _client.ListQueuesAsync(queue, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:QueueExistsAsync:HttpRequestException", queue);

            var exists = response.QueueUrls.Any(url => url[(url.LastIndexOf('/') + 1)..].Equals(queue, StringComparison.OrdinalIgnoreCase));
            return exists;
        }

        public async Task<string> CreateQueueAsync(string queue, CancellationToken token = default)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var entered = await _createSemaphore.WaitAsync(5000, token);

            // TODO: Find proper exception to throw and add to string resources
            if (!entered) throw new Exception("Queue creation aborted to avoid deadlock in critical section of code.");

            try
            {
                var response = await _client.CreateQueueAsync(queue, token);
                Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:CreateQueueAsync:HttpRequestException", queue);

                // Give AWS time to guarantee queue creation
                await Task.Delay(1200, token);

                return response.QueueUrl;
            }
            finally
            {
                _createSemaphore.Release();
            }
        }
        
        public async Task DeleteQueueAsync(string queue, CancellationToken token = default)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            
            var listResponse = await _client.ListQueuesAsync(queue, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(listResponse.HttpStatusCode, "SqsQueueManager:QueueExistsAsync:HttpRequestException", queue);

            var url = listResponse.QueueUrls.SingleOrDefault(url => url[(url.LastIndexOf('/') + 1)..].Equals(queue, StringComparison.OrdinalIgnoreCase));
            if (url == null) return;

            var deleteResponse = await _client.DeleteQueueAsync(url, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(deleteResponse.HttpStatusCode, "SqsQueueManager:DeleteQueueAsync:HttpRequestException", queue);
        }
        
        public async Task PurgeQueueAsync(string queue, CancellationToken token = default)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var listResponse = await _client.ListQueuesAsync(queue, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(listResponse.HttpStatusCode, "SqsQueueManager:QueueExistsAsync:HttpRequestException", queue);

            var url = listResponse.QueueUrls.SingleOrDefault(u => u[(u.LastIndexOf('/') + 1)..].Equals(queue, StringComparison.OrdinalIgnoreCase));
            if (url == null) return;

            var purgeResponse = await _client.PurgeQueueAsync(url, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(purgeResponse.HttpStatusCode, "SqsQueueManager:PurgeQueueAsync:HttpRequestException", queue);
        }
        
        public async IAsyncEnumerable<string> ListQueuesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            var response = await _client.ListQueuesAsync(String.Empty, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:ListQueuesAsync:HttpRequestException");

            foreach (var url in response.QueueUrls)
            {
                if (token.IsCancellationRequested) yield break;
                yield return url[(url.LastIndexOf('/') + 1)..];
            }
        }

        public async Task<string> GetQueueUrlAsync(string queue, CancellationToken token = default)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var listResponse = await _client.ListQueuesAsync(queue, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(listResponse.HttpStatusCode, "SqsQueueManager:GetQueueUrlAsync:HttpRequestException", queue);

            var url = listResponse.QueueUrls.SingleOrDefault(url => url[(url.LastIndexOf('/') + 1)..].Equals(queue, StringComparison.OrdinalIgnoreCase));
            return url;
        }
    }
}
