using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Library.Hosting
{
    public class MessageWorkerConfiguration
    {
        public static readonly MessageWorkerConfiguration Default = new();

        public bool Enabled { get; set; } = true;
    }
}