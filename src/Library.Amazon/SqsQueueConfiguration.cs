using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Amazon
{
    public class SqsQueueConfiguration
    {
        public string QueueName { get; set; } = default!;
        public string QueueUrl { get; set; } = default!;
        public int WaitTimeSeconds { get; set; } = 5;
        public int VisibilityTimeout { get; set; } = 10;
    }
}
