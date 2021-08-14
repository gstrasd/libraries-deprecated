using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public abstract class Worker : IHostedService, IDisposable
    {
        protected Worker(string name)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        internal Guid Id { get; }

        public string Name { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
