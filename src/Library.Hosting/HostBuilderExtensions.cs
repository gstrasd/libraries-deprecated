using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Library.Hosting
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseMinimumConfiguration(this IHostBuilder builder)
        {
            builder.ConfigureHostConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddEnvironmentVariables("DOTNET_");
            });

            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                configuration
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", false)
                    .AddEnvironmentVariables();
            });

            return builder;
        }

        public static IHostBuilder UseDefaultConfiguration(this IHostBuilder builder)
        {
            builder
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureHostConfiguration(configurationBuilder =>
                {
                    configurationBuilder.AddEnvironmentVariables("DOTNET_");
                })
                .ConfigureAppConfiguration((context, configuration) =>
                {
                    var env = context.HostingEnvironment;
                    var reloadOnChange = context.Configuration.GetValue("hostBuilder:reloadConfigOnChange", true);

                    configuration
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange);

                    if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName))
                    {
                        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                        if (appAssembly != null)
                        {
                            configuration.AddUserSecrets(appAssembly, optional: true);
                        }
                    }

                    configuration.AddEnvironmentVariables();
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
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
