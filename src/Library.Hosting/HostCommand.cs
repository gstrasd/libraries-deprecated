using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Library.Hosting.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public class HostCommand : RootCommand
    {
        public HostCommand(IHostBuilder hostBuilder) : this(hostBuilder, _ => _, default, default)
        {
        }

        public HostCommand(IHostBuilder hostBuilder, ISetupBuilder setupBuilder) : this(hostBuilder, _ => _, setupBuilder, _ => _)
        {
        }

        public HostCommand(Func<IHostBuilder, IHostBuilder> configureHost) : this(new HostBuilder(), configureHost, default, default)
        {
        }

        public HostCommand(Func<IHostBuilder, IHostBuilder> configureHost, Func<ISetupBuilder, ISetupBuilder> configureSetup) : this(new HostBuilder(), configureHost, new SetupBuilder(), configureSetup)
        {
        }

        internal HostCommand(IHostBuilder hostBuilder, Func<IHostBuilder, IHostBuilder> configureHost, ISetupBuilder setupBuilder, Func<ISetupBuilder, ISetupBuilder> configureSetup) : base("Sets host options and executes this application.")
        {
            if (hostBuilder == null) throw new ArgumentNullException(nameof(hostBuilder));
            if (configureHost == null) throw new ArgumentNullException(nameof(configureHost));

            AddOption(
                new Option<string>(new[] { "--applicationName", "--app" }, "Defines the application name.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<string>(new[] { "--environment", "--env" }, "Defines the application environment.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<string>(new[] { "--contentRoot", "--root" }, "Defines the content root path.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false,
                }
            );

            Handler = CommandHandler.Create(async (string applicationName, string environment, string contentRoot) =>
            {
                hostBuilder.ConfigureHostConfiguration(config =>
                {
                    // Add hosting command line options
                    var dictionary = new Dictionary<string, string>();
                    if (!String.IsNullOrWhiteSpace(applicationName)) dictionary.Add("applicationName", applicationName);
                    if (!String.IsNullOrWhiteSpace(environment)) dictionary.Add("environment", environment);
                    if (!String.IsNullOrWhiteSpace(contentRoot)) dictionary.Add("contentRoot", applicationName);
                    if (dictionary.Any()) config.AddInMemoryCollection(dictionary);
                });

                var host = configureHost(hostBuilder).Build();
                await host.StartAsync();
            });

            if (setupBuilder != default && configureSetup != default) AddCommand(new SetupCommand(setupBuilder, configureSetup));
        }
    }
}
