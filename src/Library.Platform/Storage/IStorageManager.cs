using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Storage
{
    public interface IStorageManager
    {
        Task<bool> ContainerExistsAsync(string container, CancellationToken token = default);
        Task CreateContainerAsync(string container, CancellationToken token = default);
        Task DeleteContainerAsync(string container, CancellationToken token = default);
        Task PurgeContainerAsync(string container, CancellationToken token = default);
        IAsyncEnumerable<string> ListContainersAsync(CancellationToken token = default);
    }
}