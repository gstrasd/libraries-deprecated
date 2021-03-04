using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting.Setup
{
    public class SetupCommand : Command
    {
        public SetupCommand(ISetupBuilder setupBuilder) : this(setupBuilder, _ => _)
        {
        }

        public SetupCommand(Func<ISetupBuilder, ISetupBuilder> configureSetup) : this(new SetupBuilder(), configureSetup)
        {
        }

        internal SetupCommand(ISetupBuilder setupBuilder, Func<ISetupBuilder, ISetupBuilder> configureSetup) : base("setup", "Performs all set up steps for this application.")
        {
            if (setupBuilder == null) throw new ArgumentNullException(nameof(setupBuilder));
            if (configureSetup == null) throw new ArgumentNullException(nameof(configureSetup));

            AddOption(
                new Option<string>(new[] {"--environment", "--env"}, "Defines the application environment.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false,
                    Argument = new Argument<string>(() => Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"))
                });

            AddOption(
                new Option<string>(new[] {"--path"}, "Defines the path where the appsettings.json files will be located. Defaults to the current directory if not specified.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false,
                    Argument = new Argument<string>(() => Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? Environment.CurrentDirectory))
                });

            Handler = CommandHandler.Create(async (string environment, string path) =>
            {
                setupBuilder.ConfigureSetupConfiguration(configurationBuilder =>
                {
                    configurationBuilder.Properties[HostDefaults.EnvironmentKey] = environment;
                    configurationBuilder.Properties[HostDefaults.ContentRootKey] = path;
                });
                var setup = configureSetup(setupBuilder).Build();
                await setup.ExecuteAsync();
            });
        }
    }
}