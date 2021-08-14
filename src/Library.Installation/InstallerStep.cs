using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Installation
{
    public class InstallerStep : IInstallerStep
    {
        private readonly Func<Task> _step;

        public InstallerStep(string name, Func<Task> step)
        {
            _step = step;
            Name = name;
        }

        public string Name { get; }

        public Task ExecuteAsync() => _step();
    }
}
