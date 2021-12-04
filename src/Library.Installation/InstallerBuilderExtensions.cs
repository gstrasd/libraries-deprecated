using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Configuration;

namespace Library.Installation
{
    public static class InstallerBuilderExtensions
    {
        public static IInstallerBuilder UseAppSettings(this IInstallerBuilder builder)
        {
            builder.ConfigureSetup((context, configurationBuilder) =>
            {
                configurationBuilder
                    .SetBasePath(context.HostingEnvironment.ContentRootPath)
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", false);
            });

            return builder;
        }

        public static IInstallerBuilder UseAutofac<T>(this IInstallerBuilder builder) where T : IModule
        {
            builder.ConfigureContainer((c, cb) =>
            {
                cb.RegisterModule((T)Activator.CreateInstance(typeof(T), c.Configuration));
            });
            return builder;
        }
    }
}