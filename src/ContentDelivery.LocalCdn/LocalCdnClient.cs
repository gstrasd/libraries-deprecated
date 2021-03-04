using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Library.Platform.Storage;
using Microsoft.AspNetCore.StaticFiles;

namespace Libraries.ContentDelivery.LocalCdn
{
    public class LocalCdnClient : IContentDeliveryClient
    {
        private static readonly FileExtensionContentTypeProvider _contentTypes = new FileExtensionContentTypeProvider();
        private readonly string _baseAddress;
        private static readonly Regex _pathSeparator = new Regex(@"[\\/]+", RegexOptions.Compiled);
        private static readonly List<string> _supportedFileTypes = new List<string>{ ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
        private readonly HttpClient _client;

        public LocalCdnClient(string baseAddress)
        {
            if (baseAddress == null) throw new ArgumentNullException(nameof(baseAddress));
            _baseAddress = baseAddress.Trim().TrimEnd('/');
            _client = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        }

        public async Task<Stream> DownloadAsync(string path, string filename)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));
            
            var extension = Path.GetExtension(filename).ToLower();
            if (!_supportedFileTypes.Contains(extension)) throw new ArgumentException($"The file extension \"{extension}\" is not supported.", nameof(filename));
            
            var fullPath = BuildFilePath(path, filename);

            return await _client.GetStreamAsync(fullPath);
        }

        public async Task<bool> UploadAsync(Stream stream, string path, string filename)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanSeek) throw new ArgumentException("The specified stream does not support seeking.", nameof(stream));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var fullPath = BuildFilePath(path, filename);
            using var content = new StreamContent(stream);
            if (!_contentTypes.TryGetContentType(filename, out var contentType)) contentType = "application/octet-stream";
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            var response = await _client.PutAsync(fullPath, content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteAsync(string path, string filename)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = filename != null ? BuildFilePath(path, filename) : BuildDirectoryPath(path);
            var response = await _client.DeleteAsync(fullPath);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<string>> ListFilesAsync(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var fullPath = BuildDirectoryPath(path);
            var json = await _client.GetStringAsync(fullPath);
            return JsonSerializer.Deserialize<List<string>>(json);
        }

        private string BuildDirectoryPath(string directory)
        {
            directory ??= String.Empty;

            return $"{_baseAddress}/api/directory/{_pathSeparator.Replace(directory.Trim(), "/").TrimStart('/').TrimEnd('/')}";
        }

        private string BuildFilePath(string path, string filename)
        {
            path ??= String.Empty;
            filename ??= String.Empty;

            return $"{_baseAddress}/api/files/{_pathSeparator.Replace(path.Trim(), "/").TrimStart('/').TrimEnd('/')}/{filename.Trim()}";
        }
    }
}
