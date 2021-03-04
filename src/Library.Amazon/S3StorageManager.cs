using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Library.Amazon.Resources;
using Library.Platform.Storage;

namespace Library.Amazon
{
    public class S3StorageManager : IStorageManager
    {
        private static readonly SemaphoreSlim _createSemaphore = new SemaphoreSlim(1, 1);       // TODO: Shouldn't this be: new SemaphoreSlim(0, 1);? What does initialCount mean?
        private readonly IAmazonS3 _client;

        public S3StorageManager(IAmazonS3 client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }

        public async Task<bool> StorageExistsAsync(string storage, CancellationToken token = default)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            // TODO: catch exception and throw one similar to exceptions thrown in other methods?
            var exists = await _client.DoesS3BucketExistAsync(storage);
            return exists;
        }

        public async Task CreateStorageAsync(string storage, CancellationToken token = default)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var entered = await _createSemaphore.WaitAsync(5000, token);

            // TODO: Find proper exception to throw and add to string resources
            if (!entered) throw new Exception("Bucket creation aborted to avoid deadlock in critical section of code.");

            try
            {
                // TODO: catch exception and throw one similar to exceptions thrown in other methods
                var exists = await _client.DoesS3BucketExistAsync(storage);
                if (exists) return;

                var request = new PutBucketRequest
                {
                    BucketName = storage,
                    UseClientRegion = false
                };

                var response = await _client.PutBucketAsync(request, token);
                Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "S3StorageManager:CreateStorageAsync:HttpRequestException", storage);

                // Give AWS time to guarantee bucket creation
                await Task.Delay(1200, token);
            }
            finally
            {
                _createSemaphore.Release();
            }
        }

        public async Task DeleteStorageAsync(string storage, CancellationToken token = default)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var response = await _client.DeleteBucketAsync(storage, token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "S3StorageManager:DeleteStorageAsync:HttpRequestException", storage);
        }

        public async Task PurgeStorageAsync(string storage, CancellationToken token = default)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));

            var block = new ActionBlock<string>(async key =>
            {
                var deleteRequest = new DeleteObjectRequest {BucketName = storage, Key = key};
                var deleteResponse = await _client.DeleteObjectAsync(deleteRequest, token);
                Resources.ExceptionHelper.ThrowOnFailedHttpRequest(deleteResponse.HttpStatusCode, "S3StorageManager:PurgeStorageAsync_DeleteObject:HttpRequestException", storage);
            });

            var listRequest = new ListObjectsV2Request {BucketName = storage, Prefix = String.Empty};

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _client.ListObjectsV2Async(listRequest, token);
                Resources.ExceptionHelper.ThrowOnFailedHttpRequest(listResponse.HttpStatusCode, "S3StorageManager:PurgeStorageAsync_ListObjects:HttpRequestException", storage);

                foreach (var s3Object in listResponse.S3Objects)
                {
                    if (token.IsCancellationRequested) break;
                    block.Post(s3Object.Key);
                }

            } while (listResponse.IsTruncated);

            block.Complete();
            await block.Completion;
        }

        public async IAsyncEnumerable<string> ListStoragesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            var response = await _client.ListBucketsAsync(token);
            Resources.ExceptionHelper.ThrowOnFailedHttpRequest(response.HttpStatusCode, "S3StorageManager:ListStoragesAsync:HttpRequestException");

            foreach (var bucket in response.Buckets)
            {
                if (token.IsCancellationRequested) yield break;
                yield return bucket.BucketName;
            }
        }
    }
}
