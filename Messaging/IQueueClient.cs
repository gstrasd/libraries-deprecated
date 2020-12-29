using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Libraries.Messaging
{
    public interface IQueueClient
    {
        string QueueName { get; }
        IAsyncEnumerable<string> ReadMessagesAsync(int messageCount = 1, CancellationToken token = default);
    }
}
