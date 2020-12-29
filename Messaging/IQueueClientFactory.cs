using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.Messaging
{
    public interface IQueueClientFactory
    {
        IQueueClient CreateQueueClient(string queueName);
    }
}
