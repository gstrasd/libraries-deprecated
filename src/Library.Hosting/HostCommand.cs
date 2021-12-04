using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public class HostCommand : RootCommand
    {
        private readonly IHostBuilder _builder;

        public HostCommand(IHostBuilder builder) : base("Sets host options and executes this application.")
        {
            _builder = builder;
            var appOption = new Option<string>(new[] { "--applicationName", "--app" }, "Defines the application name.")
            {
                AllowMultipleArgumentsPerToken = false,
                IsRequired = false
            };

            var envOption = new Option<string>(new[] { "--environment", "--env" }, "Defines the application environment.")
            {
                AllowMultipleArgumentsPerToken = false,
                IsRequired = false
            };

            AddOption(appOption);
            AddOption(envOption);

            Handler = CommandHandler.Create((Action<string, string>)Invoke);
        }

        private void Invoke(string applicationName, string environment)
        {
            _builder
                .ConfigureHostConfiguration(config =>
                {
                    // Add hosting command line options
                    var dictionary = new Dictionary<string, string>();
                    if (!String.IsNullOrWhiteSpace(applicationName)) dictionary.Add("applicationName", applicationName);
                    if (!String.IsNullOrWhiteSpace(environment)) dictionary.Add("environment", environment);
                    if (dictionary.Any()) config.AddInMemoryCollection(dictionary);
                });

            using var host = _builder.Build();
            host.Run();
        }
    }
}
