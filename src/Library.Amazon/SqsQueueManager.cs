using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Library.Http;
using Library.Queuing;
using Library.Amazon.Resources;

namespace Library.Amazon
{
    public class SqsQueueManager : IQueueManager
    {
        private readonly IAmazonSQS _client;

        public SqsQueueManager(IAmazonSQS client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }
        
        public async Task<bool> QueueExistsAsync(string queue, CancellationToken token = default)
        {
            var response = await _client.GetQueueUrlAsync(queue, token);
            if (response.HttpStatusCode == HttpStatusCode.NotFound) return false;
            ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:QueueExistsAsync:HttpRequestException", queue);

            return true;
        }

        public async Task<string> CreateQueueAsync(string queue, CancellationToken token = default)
        {
            var mutex = new Mutex(true, $"AWS:queue:{queue}");

            try
            {
                mutex.WaitOne(5000);

                // TODO: Optimize this. QueueExistsAsync internally calls GetQueueUrlAsync
                var exists = await QueueExistsAsync(queue, token);
                if (exists)
                {
                    // TODO: don't call this method, but perform the steps here
                    return await GetQueueUrlAsync(queue, token);
                }

                var response = await _client.CreateQueueAsync(queue, token);
                ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:CreateQueueAsync:HttpRequestException", queue);

                // Give AWS time to guarantee queue creation
                await Task.Delay(1200, token);

                return response.QueueUrl;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
        
        public async Task DeleteQueueAsync(string queue, CancellationToken token = default)
        {
            // TODO: don't call this method, but perform the steps here
            var url = await GetQueueUrlAsync(queue, token);
            var response = await _client.DeleteQueueAsync(url, token);
            ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:DeleteQueueAsync:HttpRequestException", queue);
        }
        
        public async Task PurgeQueueAsync(string queue, CancellationToken token = default)
        {
            // TODO: don't call this method, but perform the steps here
            var url = await GetQueueUrlAsync(queue, token);
            var response = await _client.PurgeQueueAsync(url, token);
            ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:PurgeQueueAsync:HttpRequestException", queue);
        }
        
        public async IAsyncEnumerable<string> ListQueuesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            var response = await _client.ListQueuesAsync(String.Empty, token);
            ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:ListQueuesAsync:HttpRequestException");

            foreach (var url in response.QueueUrls)
            {
                if (token.IsCancellationRequested) yield break;
                yield return url[(url.LastIndexOf('/') + 1)..];
            }
        }

        public async Task<string> GetQueueUrlAsync(string queue, CancellationToken token = default)
        {
            var response = await _client.GetQueueUrlAsync(queue, token);
            ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "SqsQueueManager:GetQueueUrlAsync:HttpRequestException", queue);

            return response.QueueUrl;
        }
    }
}
