using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace Library.Installation
{
    public class Installer : IInstaller
    {
        private readonly IEnumerable<IInstallerStep> _steps;

        internal Installer(IEnumerable<IInstallerStep> steps)
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
                Console.WriteLine();
            }

            colors.Push(Console.ForegroundColor);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Setup complete.");
            Console.ForegroundColor = colors.Pop();

            Console.WriteLine();
            Console.WriteLine("Press any key to terminate application.");
            Console.ReadKey();
        }

        public static IInstallerBuilder CreateDefaultBuilder()
        {
            var installerBuilder = new InstallerBuilder();

            installerBuilder.ConfigureContainer((context, builder) =>
            {
                builder.Register(c => new Installer(c.Resolve<IEnumerable<IInstallerStep>>())).As<IInstaller>();
            });

            return installerBuilder;
        }
    }
};