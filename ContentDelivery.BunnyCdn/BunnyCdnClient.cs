using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.StaticFiles;

namespace Libraries.ContentDelivery.BunnyCdn
{
    public class BunnyCdnClient : IContentDeliveryClient, IDisposable
    {
        private static readonly FileExtensionContentTypeProvider _contentTypes = new FileExtensionContentTypeProvider();
        private static readonly Regex _pathSeparator = new Regex(@"[\\/]+", RegexOptions.Compiled);
        private static readonly List<string> _supportedFileTypes = new List<string>{ ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
        private readonly string _baseAddress;
        private readonly string _storageZoneName;
        private readonly string _apiAccessKey;
        private readonly HttpClient _client;
        private bool _disposed;

        public BunnyCdnClient(string baseAddress, string storageZoneName, string apiAccessKey)
        {
            if (baseAddress == null) throw new ArgumentNullException(nameof(baseAddress));
            if (storageZoneName == null) throw new ArgumentNullException(nameof(storageZoneName));
            if (apiAccessKey == null) throw new ArgumentNullException(nameof(apiAccessKey));
            _baseAddress = baseAddress.Trim().TrimEnd('/');
            _storageZoneName = storageZoneName;
            _apiAccessKey = apiAccessKey;
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            _client.DefaultRequestHeaders.Add("AccessKey", _apiAccessKey);
        }

        ~BunnyCdnClient()
        {
            Dispose(false);
        }

        public async Task<Stream> DownloadAsync(string path, string filename)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Cannot communicate with CDN using disposed client.");
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            
            var extension = Path.GetExtension(filename).ToLower();
            if (!_supportedFileTypes.Contains(extension)) throw new ArgumentException($"The file extension \"{extension}\" is not supported.", nameof(filename));
            
            var fullPath = BuildPath(path, filename);

            return await _client.GetStreamAsync(fullPath);
        }

        public async Task<bool> UploadAsync(Stream stream, string path, string filename)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Cannot communicate with CDN using disposed client.");
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanSeek) throw new ArgumentException("The specified stream does not support seeking.", nameof(stream));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            

            // Generate checksum
            using var sha = new SHA256Managed();
            var checksum = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", String.Empty);
            stream.Position = 0L;

            // Build request
            var fullPath = BuildPath(path, filename);
            using var content = new StreamContent(stream);
            if (!_contentTypes.TryGetContentType(filename, out var contentType)) contentType = "application/octet-stream";
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            var request = new HttpRequestMessage(HttpMethod.Put, fullPath) { Content = content };
            request.Headers.Add("Checksum", checksum);

            // Upload file
            var response = await _client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string path, string filename)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Cannot communicate with CDN using disposed client.");
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var fullPath = BuildPath(path, filename);
            var response = await _client.DeleteAsync(fullPath);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> ListFilesAsync(string path)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name, "Cannot communicate with CDN using disposed client.");
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = BuildPath(path);
            var json = await _client.GetStringAsync(fullPath);
            var objects = JsonSerializer.Deserialize<StorageObject[]>(json);

            return objects.Select(o => o.FullPath).ToList();
        }

        private string BuildPath(string path, string filename = null)
        {
            path ??= String.Empty;
            filename ??= String.Empty;

            return $"{_baseAddress}/{_storageZoneName}/{_pathSeparator.Replace(path.Trim(), "/").TrimStart('/').TrimEnd('/')}/{filename.Trim()}";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _client?.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
