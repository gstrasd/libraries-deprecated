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
        string StorageName { get; }
        Task<bool> ObjectExistsAsync(string scope, string name, CancellationToken token = default);
        Task<bool> ScopeExistsAsync(string scope, CancellationToken token = default);
        IAsyncEnumerable<(string Scope, string Name)> ListObjectsAsync(string storage, CancellationToken token = default);
        IAsyncEnumerable<string> ListScopesAsync(CancellationToken token = default);
        IAsyncEnumerable<(string Scope, string Name, Stream Value, IList<KeyValuePair<string, string>> Metadata)> LoadObjectsAsync(string scope, CancellationToken token = default);
        Task<(T Value, IList<KeyValuePair<string, string>> Metadata)> LoadObjectAsync<T>(string scope, string name, CancellationToken token = default);
        Task SaveObjectAsync<T>(string scope, string name, T value, List<KeyValuePair<string, string>> metadata = default, CancellationToken token = default);
        Task DeleteObjectAsync(string scope, string name, CancellationToken token = default);
        Task PurgeScopeAsync(string scope, CancellationToken token = default);
    }
}
