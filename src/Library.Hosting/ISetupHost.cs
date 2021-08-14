using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public interface ISetupHost
    {
        void ConfigureHostBuilder(IHostBuilder builder) { }
        void ConfigureHostConfiguration(IConfigurationBuilder builder) { }
        void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder builder) { }
        void ConfigureServices(HostBuilderContext context, IServiceCollection services) { }
        void ConfigureContainer(HostBuilderContext context, ContainerBuilder builder) { }
    }
}