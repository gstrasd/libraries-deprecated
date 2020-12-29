using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Library.Queuing
{
    public interface IQueueClient
    {
        IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, CancellationToken token = default);
        // TODO: write message
        // TODO: write messages in bulk
    }
}
