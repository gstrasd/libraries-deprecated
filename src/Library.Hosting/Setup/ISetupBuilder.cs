using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting.Setup
{
    public interface ISetupBuilder
    {
        ISetupBuilder ConfigureSetupConfiguration(Action<IConfigurationBuilder> configure);
        ISetupBuilder ConfigureSetup(Action<HostBuilderContext, IConfigurationBuilder> configure);
        ISetupBuilder UseAutofac(Action<HostBuilderContext, ContainerBuilder> configure);
        ISetup Build();
    }
}