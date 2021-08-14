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
using Serilog.Sinks.Elasticsearch;

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

                configuration.AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"ApplicationName", context.HostingEnvironment.ApplicationName},
                    {"ContentRootPath", context.HostingEnvironment.ContentRootPath},
                    {"EnvironmentName", context.HostingEnvironment.EnvironmentName}
                });
            });

            return builder;
        }

        public static IHostBuilder UseDefaultConfiguration(this IHostBuilder builder)
        {
            builder
               // .UseContentRoot(Directory.GetCurrentDirectory())
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
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"ApplicationName", env.ApplicationName},
                       // {"ContentRootPath", env.ContentRootPath},
                        {"EnvironmentName", env.EnvironmentName}
                    });
                })
                .UseDefaultServiceProvider((context, options) =>
                {
                    var isDevelopment = context.HostingEnvironment.IsDevelopment();
                    options.ValidateScopes = isDevelopment;
                    options.ValidateOnBuild = isDevelopment;
                });

            return builder;
        }
    }
}
