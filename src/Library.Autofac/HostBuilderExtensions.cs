using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Module = Autofac.Module;

namespace Library.Autofac
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseAutofac(this IHostBuilder builder)
        {
            builder.UseServiceProviderFactory(new AutofacServiceProviderFactory());

            return builder;
        }

        public static IHostBuilder UseAutofac(this IHostBuilder builder, Action<HostBuilderContext, ContainerBuilder> configure)
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer(configure);

            return builder;
        }

        public static IHostBuilder UseAutofac<T>(this IHostBuilder builder) where T : IModule
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer((HostBuilderContext c, ContainerBuilder cb) =>
                {
                    cb.RegisterModule((T)Activator.CreateInstance(typeof(T), c.Configuration));
                });

            return builder;
        }
    }
}
