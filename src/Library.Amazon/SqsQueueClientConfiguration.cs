using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Amazon
{
    public class SqsQueueClientConfiguration
    {
        public string QueueUrl { get; set; }        // TODO: Change type to Uri
        public int WaitTimeSeconds { get; set; } = 5;
        public int VisibilityTimeout { get; set; } = 10;
    }
}
