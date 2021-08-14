using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public class HostCommand : RootCommand
    {
        private readonly ISetupHost[] _setupHosts;

        public HostCommand(params ISetupHost[] setups) : this()
        {
            if (setups == default) throw new ArgumentNullException(nameof(setups));
            if (!setups.Any()) throw new ArgumentException("No arguments were specified.", nameof(setups));
            if (setups.Any(t => t == default)) throw new ArgumentNullException(nameof(setups), "All arguments must not be null.");

            _setupHosts = setups;
        }

        public HostCommand(params Type[] setups) : this()
        {
            if (setups == default) throw new ArgumentNullException(nameof(setups));
            if (!setups.Any()) throw new ArgumentException("No arguments were specified.", nameof(setups));
            if (setups.Any(t => t == default)) throw new ArgumentNullException(nameof(setups), "All arguments must not be null.");
            if (setups.Any(t => !typeof(ISetupHost).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo()))) throw new ArgumentException($"All arguments must be of a type that implements {nameof(ISetupHost)}.", nameof(setups));

            _setupHosts = (
                    from c in setups
                    from ctor in c.GetConstructors(BindingFlags.Public)
                    where
                        !ctor.IsStatic
                        && !ctor.GetParameters().Any()
                    select ctor.Invoke(default)
                )
                .Cast<ISetupHost>()
                .ToArray();

            if (_setupHosts.Length != setups.Length) throw new ArgumentException("All arguments must be of a type that contains a default constructor.", nameof(setups));
        }

        private HostCommand() : base("Sets host options and executes this application.")
        {
            var appOption = new Option<string>(new[] { "--applicationName", "--app" }, "Defines the application name.")
            {
                AllowMultipleArgumentsPerToken = false,
                IsRequired = false
            };

            var envOption = new Option<string>(new[] { "--environment", "--env" }, "Defines the application environment.")
            {
                AllowMultipleArgumentsPerToken = false,
                IsRequired = false
            };

            AddOption(appOption);
            AddOption(envOption);

            Handler = CommandHandler.Create((Func<string, string, Task>)InvokeAsync);
        }

        private async Task InvokeAsync(string applicationName, string environment)
        {
            var builder = new HostBuilder()
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureHostConfiguration(config =>
                {
                    // Add hosting command line options
                    var dictionary = new Dictionary<string, string>();
                    if (!String.IsNullOrWhiteSpace(applicationName)) dictionary.Add("applicationName", applicationName);
                    if (!String.IsNullOrWhiteSpace(environment)) dictionary.Add("environment", environment);
                    if (dictionary.Any()) config.AddInMemoryCollection(dictionary);
                });

            foreach (var c in _setupHosts)
            {
                c.ConfigureHostBuilder(builder);
                builder.ConfigureHostConfiguration(c.ConfigureHostConfiguration);
                builder.ConfigureAppConfiguration(c.ConfigureAppConfiguration);
                builder.ConfigureServices(c.ConfigureServices);
                builder.ConfigureContainer<ContainerBuilder>(c.ConfigureContainer);
            }

            var host = builder.Build();
            await host.StartAsync();
        }
    }
}
