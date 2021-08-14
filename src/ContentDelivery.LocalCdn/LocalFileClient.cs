using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Platform.Storage;

namespace Library.ContentDelivery.LocalCdn
{
    public class LocalFileClient : IContentDeliveryClient
    {
        private readonly string _rootPath;

        public LocalFileClient(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException(nameof(rootPath));

            rootPath = Path.GetFullPath(rootPath);
            if (!Directory.Exists(rootPath)) throw new DirectoryNotFoundException($"The path \"{rootPath}\" was not found.");

            _rootPath = rootPath;
        }

        public async Task<Stream> DownloadAsync(string path, string filename, CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var fullPath = Path.Combine(_rootPath, path, filename);
            if (!File.Exists(fullPath)) throw new FileNotFoundException("File does not exist.", fullPath);

            var stream = new MemoryStream();
            await using var file = File.OpenRead(fullPath);
            await file.CopyToAsync(stream, token);

            return stream;
        }

        public async Task<bool> UploadAsync(Stream stream, string path, string filename, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var directory = Path.Combine(_rootPath, path);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            var fullPath = Path.Combine(_rootPath, path, filename);
            await using var fileStream = File.Create(fullPath);
            await stream.CopyToAsync(fileStream, token);

            return true;
        }

        public Task<bool> DeleteAsync(string path, string filename, CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            var fullPath = Path.Combine(_rootPath, path, filename);
            if (!File.Exists(fullPath)) return Task.FromResult(true);

            File.Delete(fullPath);
            return Task.FromResult(true);
        }

        public async IAsyncEnumerable<string> ListFilesAsync(string path, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            var directory = Path.Combine(_rootPath, path);
            if (!Directory.Exists(directory)) yield break;

            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                if (token.IsCancellationRequested) yield break;
                yield return Path.GetFileName(file);
            }
        }
    }
}