using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IStorageClient
    {
        string Store { get; }
        Task<bool> ExistsAsync(string path, CancellationToken token = default);
        IAsyncEnumerable<string> ListAsync(string path, CancellationToken token = default);
        Task<Stream> LoadAsync(string path, CancellationToken token = default);
        Task SaveAsync(string path, Stream stream, CancellationToken token = default);
        Task DeleteAsync(string path, CancellationToken token = default);
        Task DeletePathAsync(string path, CancellationToken token = default);
    }
}
