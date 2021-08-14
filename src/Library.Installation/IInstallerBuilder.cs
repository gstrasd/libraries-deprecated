using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Installation
{
    public interface IInstallerBuilder
    {
        IInstallerBuilder ConfigureSetupConfiguration(Action<IConfigurationBuilder> configure);
        IInstallerBuilder ConfigureSetup(Action<HostBuilderContext, IConfigurationBuilder> configure);
        IInstallerBuilder ConfigureContainer(Action<HostBuilderContext, ContainerBuilder> configure);
        IInstaller Build();
    }
}