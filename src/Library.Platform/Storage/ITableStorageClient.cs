using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface ITableStorageClient
    {
        Task<bool> ExistsAsync(object[] keys, CancellationToken token = default);
        Task SaveAsync<T>(T entity, CancellationToken token = default);
        Task<T> FindAsync<T>(object[] keys, CancellationToken token = default);
        Task RemoveAsync(object[] keys, CancellationToken token = default);
    }
}