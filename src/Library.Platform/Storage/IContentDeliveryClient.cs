using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IContentDeliveryClient
    {
        Task<Stream> DownloadAsync(string path, string filename, CancellationToken token = default);
        Task<bool> UploadAsync(Stream stream, string path, string filename, CancellationToken token = default);
        Task<bool> DeleteAsync(string path, string filename, CancellationToken token = default);
        IAsyncEnumerable<string> ListFilesAsync(string path, CancellationToken token = default);
    }
}