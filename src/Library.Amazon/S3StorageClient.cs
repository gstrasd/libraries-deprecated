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
using Library.Http;
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

        public string StorageName => _configuration.BucketName;

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
            if (!response.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code {(int)response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to determine if an object with the given \"{key}\" key in the \"{_configuration.BucketName}\" bucket exists.");
            }

            return response.KeyCount > 0;
        }

        public async Task<bool> ScopeExistsAsync(string scope, CancellationToken token = default)
        {
            EnsureNotDisposed();
            NormalizeScope(ref scope);

            var request = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = scope,
            };

            var response = await _client.ListObjectsV2Async(request, token);
            if (!response.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code {(int)response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to determine if the given \"{scope}\" storage in the \"{_configuration.BucketName}\" bucket exists.");
            }

            return response.KeyCount > 0;
        }

        public async IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(string storage, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();
            NormalizeScope(ref storage);

            var request = new ListObjectsV2Request
            {
                BucketName = _configuration.BucketName,
                Prefix = storage,
            };

            ListObjectsV2Response response;
            do
            {
                response = await _client.ListObjectsV2Async(request, token);
                if (!response.HttpStatusCode.IsSuccess())
                {
                    throw new HttpRequestException($"Response status code {(int) response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to retrieve a list of objects with the given \"{storage}\" storage in the \"{_configuration.BucketName}\" bucket.");
                }
                
                foreach (var s3Object in response.S3Objects)
                {
                    if (token.IsCancellationRequested) yield break;

                    var key = s3Object.Key;
                    if (key.Contains("/")) yield return (key[..(key.LastIndexOf("/") - 1)], key[key.LastIndexOf("/")..]);
                    else yield return (String.Empty, key);
                }
            } while (response.IsTruncated);
        }

        public async IAsyncEnumerable<string> ListScopesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var scopes = new List<string>(1000);
            var objects = ListObjectsAsync(String.Empty, token);

            await foreach (var @object in objects.WithCancellation(token))
            {
                scopes.Add(@object.Scope);
            }

            scopes.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var scope in scopes.Distinct(StringComparer.Ordinal))
            {
                if (token.IsCancellationRequested) yield break;

                yield return scope;
            }
        }

        public async IAsyncEnumerable<(string Scope, string Name, Stream Value, IList<KeyValuePair<string, string>> Metadata)> LoadObjectsAsync(string scope, [EnumeratorCancellation] CancellationToken token = default)
        {
            EnsureNotDisposed();

            var block = new TransformBlock<(string Scope, string Name), (string Scope, string Name, Stream Value, IList<KeyValuePair<string, string>> Metadata)>(
                async o =>
                {
                    var (value, metadata) = await LoadObjectAsync<Stream>(o.Scope, o.Name, token);
                    return (o.Scope, o.Name, value, metadata);
                });
            var objects = ListObjectsAsync(scope, token);

            await foreach (var @object in objects.WithCancellation(token)) await block.SendAsync(@object, token);
            block.Complete();

            while (await block.OutputAvailableAsync(token))
            {
                if (token.IsCancellationRequested) yield break;

                yield return await block.ReceiveAsync(token);
            }
        }

        public async Task<(T Value, IList<KeyValuePair<string, string>> Metadata)> LoadObjectAsync<T>(string scope, string name, CancellationToken token = default)
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

            GetObjectResponse response = null;
            try
            {
                response = await _client.GetObjectAsync(request, token);
            }
            catch (Exception e)
            {
                return default;
            }

            if (!response.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code {(int)response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to load an object with the given \"{key}\" key in the \"{_configuration.BucketName}\" bucket.");
            }
            
            var metadata = response.Metadata?.Keys.Select(key => new KeyValuePair<string, string>(key, response.Metadata[key])).ToList() ?? new List<KeyValuePair<string, string>>();

            var targetType = typeof(T);
            if (targetType == typeof(Stream)) return ((T)(object)response.ResponseStream, metadata);

            var sourceType = Type.GetType(metadata.FirstOrDefault(pair => pair.Key.Equals("Type")).Value);
            if (sourceType != null && targetType != sourceType && !sourceType.IsSubclassOf(targetType))
            {
                throw new InvalidCastException($"Requested object is not of the specified type ({targetType.Name}).");
            }

            await using (response.ResponseStream)
            {
                var formatter = new BinaryFormatter();
                var value = (T) formatter.Deserialize(response.ResponseStream);
                return (value, metadata);
            }
        }

        public async Task SaveObjectAsync<T>(string scope, string name, T value, List<KeyValuePair<string, string>> metadata = default, CancellationToken token = default)
        {
            EnsureNotDisposed();

            if (name == null) throw new ArgumentNullException(nameof(name));

            name = name.Trim();
            if (name.Length == 0) throw new ArgumentException("No name was specified.", nameof(name));
            if (typeof(T).IsValueType && value == null) throw new ArgumentNullException(nameof(value));

            var key = BuildKey(scope, name);
            var request = new PutObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key
            };
            metadata?.ForEach(pair => request.Metadata.Add(pair.Key, pair.Value));
            request.Metadata.Add("Type", typeof(T).AssemblyQualifiedName);

            if (typeof(T) == typeof(Stream))
            {
                request.InputStream = (Stream)(object)value;
                request.AutoCloseStream = false;
                request.AutoResetStreamPosition = true;
                var extension = key[key.LastIndexOf('.')..];
                request.ContentType = MimeTypes.ByExtension[extension].FirstOrDefault()?.ContentType ?? "application/octet-stream";
                request.Metadata.Add("Extension", extension);
            }
            else
            {
                var formatter = new BinaryFormatter();
                await using var stream = new MemoryStream();
                formatter.Serialize(stream, value);
                stream.Position = 0;
                request.InputStream = stream;
                request.ContentType = "application/octet-stream";
            }

            var response = await _client.PutObjectAsync(request, token);
            if (!response.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code {(int)response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to save an object with the given \"{key}\" key in the \"{_configuration.BucketName}\" bucket.");
            }
        }

        public async Task DeleteObjectAsync(string scope, string name, CancellationToken token = default)
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
            
            var response = await _client.DeleteObjectAsync(request, token);
            if (!response.HttpStatusCode.IsSuccess())
            {
                throw new HttpRequestException($"Response status code {(int)response.HttpStatusCode} ({response.HttpStatusCode.GetDescription()}) indicates that this storage client failed to delete an object with the given \"{key}\" key in the \"{_configuration.BucketName}\" bucket.");
            }
        }

        public async Task PurgeScopeAsync(string scope, CancellationToken token = default)
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
            NormalizeScope(ref scope);
            return $"{scope}{name.Trim()}";
        }

        private static void NormalizeScope(ref string scope)
        {
            if (String.IsNullOrWhiteSpace(scope)) scope = String.Empty;
            scope = Regex.Replace(scope.Trim(), @"[\\/]+", "/");
            if (scope == "/") scope = String.Empty;
            if (!scope.EndsWith("/")) scope += "/";
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