using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3.Util;
using Library.Amazon.Resources;
using Library.Net;
using Library.Platform.Storage;

namespace Library.Amazon
{
    public class S3StorageClient : IStorageClient, IDisposable
    {
        private readonly IAmazonS3 _client;
        private readonly S3StorageClientConfiguration _configuration;
        private bool _disposed;

        public S3StorageClient(IAmazonS3 client, S3StorageClientConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.BucketName == null) throw new ArgumentNullException(nameof(configuration.BucketName));
            if (configuration.BucketName.Trim().Length == 0) throw new ArgumentException("No bucket name was provided.", nameof(configuration.BucketName));

            _client = client;
            _configuration = configuration;
        }

        ~S3StorageClient()
        {
            Dispose(false);
        }

        public string Container => _configuration.BucketName;

        public Task<bool> ObjectExistsAsync(string name, CancellationToken token = default) => ObjectExistsAsync(null, name, token);

        public async Task<bool> ObjectExistsAsync(string scope, string name, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var key = BuildKey(scope, name);
            var request = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = key,
            };

            var response = await _client.ListObjectsV2Async(request, token);

            return response.KeyCount > 0;
        }

        public IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(CancellationToken token = default) => ListObjectsAsync(null, token);

        public async IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(string scope, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();
            var prefix = NormalizeScope(scope);

            var request = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = prefix,
            };

            ListObjectsV2Response response;
            do
            {
                response = await _client.ListObjectsV2Async(request, token);

                foreach (var @object in response.S3Objects)
                {
                    if (token.IsCancellationRequested) yield break;

                    var key = @object.Key;
                    if (key.Contains("/")) yield return (key[..(key.LastIndexOf("/") - 1)], key[key.LastIndexOf("/")..]);
                    else yield return (String.Empty, key);
                }
            } while (response.IsTruncated);
        }

        public Task<Stream> ReadObjectAsync(string name, CancellationToken token = default) => ReadObjectAsync(null, name, token);

        public Task<Stream> ReadObjectAsync(string scope, string name, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (name == null) throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            if (name.Length == 0) throw new ArgumentException("No name was specified.", nameof(name));

            var key = BuildKey(scope, name);
            var request = new GetObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key,
            };

            return _client.GetObjectAsync(request, token).ContinueWith(t => t.Result?.ResponseStream, token);
        }

        public Task WriteObjectAsync(string name, Stream stream, CancellationToken token = default) => WriteObjectAsync(null, name, stream, token);

        public Task WriteObjectAsync(string scope, string name, Stream stream, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (name == null) throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            if (name.Length == 0) throw new ArgumentException("No name was specified.", nameof(name));

            var key = BuildKey(scope, name);
            var request = new PutObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key,
                InputStream = stream,
                AutoCloseStream = false,
                AutoResetStreamPosition = true
            };

            var extension = key[key.LastIndexOf('.')..];
            request.ContentType = MimeTypes.ByExtension[extension].FirstOrDefault()?.ContentType ?? "application/octet-stream";

            return _client.PutObjectAsync(request, token);
        }

        public Task DeleteObjectAsync(string name, CancellationToken token = default) => DeleteObjectAsync(null, name, token);

        public Task DeleteObjectAsync(string scope, string name, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (name == null) throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            if (name.Length == 0) throw new ArgumentException("No name was specified.", nameof(name));

            var key = BuildKey(scope, name);
            var request = new DeleteObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key
            };

            return _client.DeleteObjectAsync(request, token);
        }

        public async Task DeleteScopeAsync(string scope, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var block = new ActionBlock<(string Scope, string Name)>(async o => await DeleteObjectAsync(o.Scope, o.Name, token));
            var objects = ListObjectsAsync(scope, token);

            await foreach (var @object in objects.WithCancellation(token)) await block.SendAsync(@object, token);
            block.Complete();

            await block.Completion;
        }

        private static string BuildKey(string scope, string name)
        {
            scope = NormalizeScope(scope);
            return $"{scope}{name.Trim()}";
        }

        private static string NormalizeScope(string scope)
        {
            if (String.IsNullOrWhiteSpace(scope)) scope = String.Empty;
            scope = Regex.Replace(scope.Trim(), @"[\\/]+", "/");
            if (scope == "/") scope = String.Empty;
            if (!scope.EndsWith("/")) scope += "/";

            return scope;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) ExceptionHelper.ThrowDisposed(nameof(S3StorageClient));
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