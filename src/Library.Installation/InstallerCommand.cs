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

namespace Library.Installation
{
    public class InstallerCommand : Command
    {
        public InstallerCommand(IInstallerBuilder installerBuilder) : this(installerBuilder, _ => _)
        {
        }

        public InstallerCommand(Func<IInstallerBuilder, IInstallerBuilder> configureSetup) : this(new InstallerBuilder(), configureSetup)
        {
        }

        internal InstallerCommand(IInstallerBuilder installerBuilder, Func<IInstallerBuilder, IInstallerBuilder> configureSetup) : base("setup", "Performs all set up steps for this application.")
        {
            if (installerBuilder == null) throw new ArgumentNullException(nameof(installerBuilder));
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
                installerBuilder.ConfigureSetupConfiguration(configurationBuilder =>
                {
                    configurationBuilder.Properties[HostDefaults.EnvironmentKey] = environment;
                    configurationBuilder.Properties[HostDefaults.ContentRootKey] = path;
                });
                var setup = configureSetup(installerBuilder).Build();
                await setup.ExecuteAsync();
            });
        }
    }
}