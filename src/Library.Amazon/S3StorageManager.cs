using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private static readonly Regex _bucketNamingRules = new Regex(@"[a-z\d][a-z\d\.-]{1,61}[a-z\d]", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly Regex _ipAddress = new Regex(@"^(?:(?:25[0-5]|2[0-4]\d|1\d{2}|\d{1,2})\.){3}(?:25[0-5]|2[0-4]\d|1\d{2}|\d{1,2})$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static readonly SemaphoreSlim _createSemaphore = new SemaphoreSlim(1, 1);       // TODO: Shouldn't this be: new SemaphoreSlim(0, 1);? What does initialCount mean?
        private readonly IAmazonS3 _client;

        public S3StorageManager(IAmazonS3 client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            _client = client;
        }

        public Task<bool> ContainerExistsAsync(string container, CancellationToken token = default)
        {
            ValidateContainerName(container);

            return _client.DoesS3BucketExistAsync(container);
        }

        public async Task CreateContainerAsync(string container, CancellationToken token = default)
        {
            ValidateContainerName(container);

            var entered = await _createSemaphore.WaitAsync(5000, token);
            if (!entered) throw new OperationCanceledException("Bucket creation aborted to avoid deadlock in critical section of code.");

            try
            {
                var exists = await _client.DoesS3BucketExistAsync(container);
                if (exists) return;

                var request = new PutBucketRequest
                {
                    BucketName = container,
                    UseClientRegion = true
                };

                await _client.PutBucketAsync(request, token);

                // Give AWS time to guarantee bucket creation
                await Task.Delay(1200, token);
            }
            finally
            {
                _createSemaphore.Release();
            }
        }

        public Task DeleteContainerAsync(string container, CancellationToken token = default)
        {
            ValidateContainerName(container);

            return _client.DeleteBucketAsync(container, token);
        }

        public async Task PurgeContainerAsync(string container, CancellationToken token = default)
        {
            ValidateContainerName(container);

            var block = new ActionBlock<string>(async key =>
            {
                var deleteRequest = new DeleteObjectRequest { BucketName = container, Key = key };
                await _client.DeleteObjectAsync(deleteRequest, token);
            });

            var listRequest = new ListObjectsV2Request { BucketName = container, Prefix = String.Empty };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _client.ListObjectsV2Async(listRequest, token);

                foreach (var s3Object in listResponse.S3Objects)
                {
                    if (token.IsCancellationRequested) break;
                    block.Post(s3Object.Key);
                }

            } while (listResponse.IsTruncated);

            block.Complete();
            await block.Completion;
        }

        public async IAsyncEnumerable<string> ListContainersAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            var response = await _client.ListBucketsAsync(token);

            foreach (var bucket in response.Buckets)
            {
                if (token.IsCancellationRequested) yield break;
                yield return bucket.BucketName;
            }
        }

        private static void ValidateContainerName(string container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (!_bucketNamingRules.IsMatch(container) || _ipAddress.IsMatch(container)) throw new ArgumentException(
                "Bucket names must adhere to the following rules: " +
                "must be between 3 and 63 characters in length; " +
                "can only consist of lowercase letters, numbers, dots (.), and hyphens (-); " +
                "must begin and end with a letter or number; " +
                "must not be formatted as an IP address.", nameof(container));
        }
    }
}
