using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Library.Installation
{
    public class InstallerBuilder : IInstallerBuilder
    {
        private readonly IConfigurationBuilder _configurationBuilder = new ConfigurationBuilder();
        private readonly ContainerBuilder _containerBuilder = new ContainerBuilder();
        private readonly List<Action<IConfigurationBuilder>> _configureSetupConfigurationActions = new List<Action<IConfigurationBuilder>>();
        private readonly List<Action<HostBuilderContext, IConfigurationBuilder>> _configureSetupActions = new List<Action<HostBuilderContext, IConfigurationBuilder>>();
        private readonly List<Action<HostBuilderContext, ContainerBuilder>> _configureContainerActions = new List<Action<HostBuilderContext, ContainerBuilder>>();

        public IInstallerBuilder ConfigureSetupConfiguration(Action<IConfigurationBuilder> configure)
        {
            _configureSetupConfigurationActions.Add(configure);
            return this;
        }

        public IInstallerBuilder ConfigureSetup(Action<HostBuilderContext, IConfigurationBuilder> configure)
        {
            _configureSetupActions.Add(configure);
            return this;
        }

        public IInstallerBuilder ConfigureContainer(Action<HostBuilderContext, ContainerBuilder> configure)
        {
            _configureContainerActions.Add(configure);
            return this;
        }

        public IInstaller Build()
        {
            // Configure setup configuration
            var configurationBuilder = new ConfigurationBuilder();
            _configureSetupConfigurationActions.ForEach(action => action(configurationBuilder));

            // Configure setup
            var context = new HostBuilderContext(new Dictionary<object, object>())
            {
                Configuration = configurationBuilder.Build(),
                HostingEnvironment = new HostingEnvironment()
            };
            if (configurationBuilder.Properties.TryGetValue(HostDefaults.ApplicationKey, out var applicationName)) context.HostingEnvironment.ApplicationName = (string)applicationName;
            if (configurationBuilder.Properties.TryGetValue(HostDefaults.ContentRootKey, out var contentRootPath)) context.HostingEnvironment.ContentRootPath = (string)contentRootPath;
            if (configurationBuilder.Properties.TryGetValue(HostDefaults.EnvironmentKey, out var environmentName)) context.HostingEnvironment.EnvironmentName = (string)environmentName;
            _configureSetupActions.ForEach(action => action(context, _configurationBuilder));
            
            // Configure dependency injection
            context.Configuration = _configurationBuilder.Build();
            _configureContainerActions.ForEach(action => action(context, _containerBuilder));
            
            // Resolve Setup class 
            var container = _containerBuilder.Build();
            var setup = container.Resolve<IInstaller>();

            return setup;
        }
    }
}