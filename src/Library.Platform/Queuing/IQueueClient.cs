using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Library.Dataflow;

namespace Library.Platform.Queuing
{
    public interface IQueueClient
    {
        string QueueName { get; }
        Task WriteRawMessageAsync(string message, CancellationToken token = default);
        Task WriteMessageAsync(string message, CancellationToken token = default);
        Task WriteMessageAsync<TMessage>(TMessage message, CancellationToken token = default) where TMessage : IMessage;
        IAsyncEnumerable<string> ReadRawMessageAsync(int messageCount = 1, CancellationToken token = default);
        IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, CancellationToken token = default);
        IAsyncEnumerable<TMessage> ReadMessagesAsync<TMessage>(int messageCount = 1, CancellationToken token = default) where TMessage : IMessage;
        // TODO: write message
        // TODO: write messages in bulk
    }
}
