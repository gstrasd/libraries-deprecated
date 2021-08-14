using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Installation
{
    public class Installer : IInstaller
    {
        private readonly IEnumerable<IInstallerStep> _steps;

        public Installer(IEnumerable<IInstallerStep> steps)
        {
            _steps = steps;
        }

        public async Task ExecuteAsync()
        {
            var colors = new Stack<ConsoleColor>();
            var stepNumber = 0;

            foreach (var step in _steps)
            {
                colors.Push(Console.ForegroundColor);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Step {++stepNumber}: {step.Name}");
                Console.ForegroundColor = colors.Pop();
                await step.ExecuteAsync();
            }

            colors.Push(Console.ForegroundColor);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Setup complete.");
            Console.ForegroundColor = colors.Pop();
        }
    }
}