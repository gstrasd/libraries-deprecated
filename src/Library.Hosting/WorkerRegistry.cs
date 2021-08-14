using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Library.Hosting
{
    public sealed class WorkerRegistry
    {
        private readonly ConcurrentDictionary<Guid, Worker> _registry = new ConcurrentDictionary<Guid, Worker>();

        internal WorkerRegistry()
        {
        }

        public void Register(Worker worker)
        {

        }
    }
}
