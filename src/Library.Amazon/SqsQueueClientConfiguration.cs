using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Amazon
{
    public class SqsQueueClientConfiguration
    {
        public string QueueUrl { get; set; }
        public int ReceiveWaitTimeSeconds { get; set; } = 5;
        public int ReceiveVisibilityTimeout { get; set; } = 10;
    }
}
