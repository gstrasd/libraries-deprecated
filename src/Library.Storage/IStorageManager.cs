using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Storage
{
    public interface IStorageManager
    {
        Task<bool> StorageExistsAsync(string storage, CancellationToken token = default);
        Task CreateStorageAsync(string storage, CancellationToken token = default);
        Task DeleteStorageAsync(string storage, CancellationToken token = default);
        Task PurgeStorageAsync(string storage, CancellationToken token = default);
        IAsyncEnumerable<string> ListStoragesAsync(CancellationToken token = default);
    }
}