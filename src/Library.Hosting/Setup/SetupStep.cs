using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Hosting.Setup
{
    public class SetupStep : ISetupStep
    {
        private readonly Func<Task> _step;

        public SetupStep(string name, Func<Task> step)
        {
            _step = step;
            Name = name;
        }

        public string Name { get; }

        public Task ExecuteAsync() => _step();
    }
}
