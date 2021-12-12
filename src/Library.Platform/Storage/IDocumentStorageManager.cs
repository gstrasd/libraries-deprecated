using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IDocumentStorageManager
    {
        Task<bool> StoreExistsAsync(string container, CancellationToken token = default);
        Task CreateStoreAsync(string container, CancellationToken token = default);
        Task DeleteStoreAsync(string container, CancellationToken token = default);
        Task PurgeStoreAsync(string container, CancellationToken token = default);
        IAsyncEnumerable<string> ListStoresAsync(CancellationToken token = default);
    }
}