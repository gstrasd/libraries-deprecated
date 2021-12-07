using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

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
                    .SetFileProvider(new PhysicalFileProvider(context.HostingEnvironment.ContentRootPath))
                    .AddJsonFile("appsettings.json", false)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", false);
            });

            return builder;
        }
    }
}