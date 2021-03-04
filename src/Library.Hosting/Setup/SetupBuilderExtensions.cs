﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Configuration;

namespace Library.Hosting.Setup
{
    public static class SetupBuilderExtensions
    {
        public static ISetupBuilder UseDefaultConfiguration(this ISetupBuilder builder)
        {
            builder.UseAutofac((context, containerBuilder) =>
            {
                containerBuilder.Register(c => new Setup(c.Resolve<IEnumerable<ISetupStep>>())).As<ISetup>();
            });

            return builder;
        }

        public static ISetupBuilder UseAppConfiguration(this ISetupBuilder builder)
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
    }
}