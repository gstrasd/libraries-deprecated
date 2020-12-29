using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Library.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Library.Hosting
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseDefaultHostConfiguration(this IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddEnvironmentVariables("DOTNET_");
            });

            return builder;
        }

        public static IHostBuilder UseDefaultAppConfiguration(this IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", false)
                    .AddEnvironmentVariables();
            });

            return builder;
        }

        public static IHostBuilder UseAutofac(this IHostBuilder builder, Action<HostBuilderContext, ContainerBuilder> configure)
        {
            builder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer(configure);

            return builder;
        }

        public static IHostBuilder UseSerilog(this IHostBuilder builder)
        {
            builder.ConfigureContainer((HostBuilderContext context, ContainerBuilder containerBuilder) =>
            {
                containerBuilder.Register(c =>
                    {
                        var logger = new LoggerConfiguration()
                            .ReadFrom.Configuration(context.Configuration)
                            .CreateLogger();

                        return logger;
                    })
                    .As<ILogger>()
                    .InstancePerDependency();
            });

            return builder;
        }
    }
}
