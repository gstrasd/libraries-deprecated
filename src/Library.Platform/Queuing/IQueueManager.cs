using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Library.Platform.Queuing
{
    public interface IQueueManager
    {
        Task<bool> QueueExistsAsync(string queue, CancellationToken token = default);
        Task<string> CreateQueueAsync(string queue, CancellationToken token = default);
        Task DeleteQueueAsync(string queue, CancellationToken token = default);
        Task PurgeQueueAsync(string queue, CancellationToken token = default);
        IAsyncEnumerable<string> ListQueuesAsync(CancellationToken token = default);
    }
}