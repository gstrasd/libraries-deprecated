using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Hosting
{
    public class MessageWorkerConfiguration
    {
        public string Name { get; set; }
        public string MessageType { get; set; }
        public bool Enabled { get; set; }
        public int ProducerCount { get; set; }
        public int ConsumerCount { get; set; }
    }
}