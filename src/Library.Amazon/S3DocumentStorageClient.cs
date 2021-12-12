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
    public class S3DocumentStorageClient : IDocumentStorageClient, IDisposable
    {
        private readonly IAmazonS3 _client;
        private readonly S3StorageClientConfiguration _configuration;
        private bool _disposed;

        public S3DocumentStorageClient(IAmazonS3 client, S3StorageClientConfiguration configuration)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.BucketName == null) throw new ArgumentNullException(nameof(configuration.BucketName));
            if (configuration.BucketName.Trim().Length == 0) throw new ArgumentException("No bucket name was provided.", nameof(configuration.BucketName));

            _client = client;
            _configuration = configuration;
        }

        ~S3DocumentStorageClient()
        {
            Dispose(false);
        }

        public string Store => _configuration.BucketName;

        public async Task<bool> DocumentExistsAsync(string path, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var request = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = path,
            };

            var response = await _client.ListObjectsV2Async(request, token);

            return response.KeyCount > 0;
        }

        public async IAsyncEnumerable<string> ListDocumentsAsync(string path, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var prefix = NormalizePath(path);
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
                    yield return @object.Key;
                }
            } 
            while (response.IsTruncated);
        }

        public Task<Stream> LoadDocumentAsync(string path, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var key = NormalizePath(path);
            var request = new GetObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key,
            };

            return _client.GetObjectAsync(request, token).ContinueWith(t => t.Result?.ResponseStream, token);
        }

        public Task SaveDocumentAsync(string path, Stream stream, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var key = NormalizePath(path);
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

        public Task DeleteDocumentAsync(string path, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var key = NormalizePath(path);
            var request = new DeleteObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key,
                
            };

            return _client.DeleteObjectAsync(request, token);
        }

        public async Task DeleteDocumentsAsync(string path, CancellationToken token = default)
        {
            EnsureNotDisposed();

            var prefix = NormalizePath(path);
            var listRequest = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = prefix,
            };

            ListObjectsV2Response listResponse;
            do
            {
                listResponse = await _client.ListObjectsV2Async(listRequest, token);

                foreach (var @object in listResponse.S3Objects)
                {
                    if (token.IsCancellationRequested) break;

                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _configuration.BucketName,
                        Key = @object.Key,
                    };
#pragma warning disable 4014
                    _client.DeleteObjectAsync(deleteRequest, token);
#pragma warning restore 4014
                }
            }
            while (listResponse.IsTruncated);
        }

        private static string NormalizePath(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) path = String.Empty;
            path = Regex.Replace(path.Trim(), @"[\\/]+", "/");
            if (path == "/") path = String.Empty;
            if (!path.EndsWith("/")) path += "/";

            return path;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void EnsureNotDisposed()
        {
            if (_disposed) ExceptionHelper.ThrowDisposed(nameof(S3DocumentStorageClient));
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