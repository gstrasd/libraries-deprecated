﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Library.Platform.Storage;
using Microsoft.AspNetCore.StaticFiles;

namespace ContentDelivery.BunnyCdn
{
    public class BunnyCdnClient : IContentDeliveryClient
    {
        private static readonly FileExtensionContentTypeProvider _contentTypes = new FileExtensionContentTypeProvider();
        private static readonly Regex _pathSeparator = new Regex(@"[\\/]+", RegexOptions.Compiled);
        private static readonly List<string> _supportedFileTypes = new List<string>{ ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
        private readonly string _baseAddress;
        private readonly string _storageZoneName;
        private readonly string _apiAccessKey;
        private readonly HttpClient _client;

        public BunnyCdnClient(string baseAddress, string storageZoneName, string apiAccessKey) 
            : this(baseAddress, storageZoneName, apiAccessKey, new HttpClient { Timeout = TimeSpan.FromSeconds(120) })
        {
        }

        public BunnyCdnClient(string baseAddress, string storageZoneName, string apiAccessKey, HttpClient client)
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

        public async Task<Stream> DownloadAsync(string path, string filename, CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            
            var extension = Path.GetExtension(filename).ToLower();
            if (!_supportedFileTypes.Contains(extension)) throw new ArgumentException($"The file extension \"{extension}\" is not supported.", nameof(filename));
            
            var fullPath = BuildPath(path, filename);

            return await _client.GetStreamAsync(fullPath);
        }

        public async Task<bool> UploadAsync(Stream stream, string path, string filename, CancellationToken token = default)
        {
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
            var response = await _client.SendAsync(request, token);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string path, string filename, CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var fullPath = BuildPath(path, filename);
            var response = await _client.DeleteAsync(fullPath, token);
            return response.IsSuccessStatusCode;
        }

        public async IAsyncEnumerable<string> ListFilesAsync(string path, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = BuildPath(path);
            var json = await _client.GetStringAsync(fullPath);
            var objects = JsonSerializer.Deserialize<StorageObject[]>(json);
            if (objects == null) throw new JsonException();

            foreach (var o in objects)
            {
                if (token.IsCancellationRequested) yield break;
                yield return o.FullPath;
            }
        }

        private string BuildPath(string path, string filename = null)
        {
            path ??= String.Empty;
            filename ??= String.Empty;

            return $"{_baseAddress}/{_storageZoneName}/{_pathSeparator.Replace(path.Trim(), "/").TrimStart('/').TrimEnd('/')}/{filename.Trim()}";
        }
    }
}