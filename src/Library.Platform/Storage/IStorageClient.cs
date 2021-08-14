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
        string Container { get; }
        Task<bool> ObjectExistsAsync(string name, CancellationToken token = default);
        Task<bool> ObjectExistsAsync(string scope, string name, CancellationToken token = default);
        IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(CancellationToken token = default);
        IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(string scope, CancellationToken token = default);
        Task<Stream> ReadObjectAsync(string name, CancellationToken token = default);
        Task<Stream> ReadObjectAsync(string scope, string name, CancellationToken token = default);
        Task WriteObjectAsync(string name, Stream stream, CancellationToken token = default);
        Task WriteObjectAsync(string scope, string name, Stream stream, CancellationToken token = default);
        Task DeleteObjectAsync(string name, CancellationToken token = default);
        Task DeleteObjectAsync(string scope, string name, CancellationToken token = default);
        Task DeleteScopeAsync(string scope, CancellationToken token = default);
    }
}
