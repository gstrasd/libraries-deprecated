using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Dataflow;

namespace Library.Platform.Queuing
{
    public interface IQueueClient
    {
        IAsyncEnumerable<string> ReadMessageAsync(string queueName, int messageCount = 1, CancellationToken token = default);
        IAsyncEnumerable<T> ReadMessageAsync<T>(int messageCount = 1, CancellationToken token = default) where T : IMessage;
        Task WriteMessageAsync(string queueName, string message, CancellationToken token = default);
        Task WriteMessageAsync<T>(T message, CancellationToken token = default) where T : IMessage;
        Task DeleteMessageAsync(string queueName, string receipt, CancellationToken token = default);
    }
}
