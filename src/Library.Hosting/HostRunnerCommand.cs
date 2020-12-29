using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Library.Hosting
{
    public class HostRunnerCommand : RootCommand
    {
        public HostRunnerCommand(Func<IHostBuilder, Task<IHost>> buildHost) : base("Sets options for the application.")
        {
            AddOption(
                new Option<string>(new []{ "--applicationName", "--app" }, "Defines the application name.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<string>(new[] { "--environment", "--env" }, "Defines the application environment.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<string>(new[] { "--contentRoot", "--content" }, "Defines the content root path.")
                {
                    AllowMultipleArgumentsPerToken = false,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<List<string>>(new[] { "--workers" }, "A list of service workers to host.")
                {
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = false
                }
            );

            AddOption(
                new Option<List<string>>(new[] { "--options" }, "A list of configuration options in a key[:childKey]=value format.")
                {
                    AllowMultipleArgumentsPerToken = true,
                    IsRequired = false
                }
            );

            Handler = CommandHandler.Create(async (HostOptions hostOptions, AppOptions appOptions) =>
            {
                var builder = new HostBuilder();
                builder.ConfigureHostConfiguration(config =>
                {
                    // Add hosting command line options
                    var dictionary = new Dictionary<string, string>();
                    if (!String.IsNullOrWhiteSpace(hostOptions?.ApplicationName)) dictionary.Add("applicationName", hostOptions.ApplicationName);
                    if (!String.IsNullOrWhiteSpace(hostOptions?.Environment)) dictionary.Add("environment", hostOptions.Environment);
                    if (!String.IsNullOrWhiteSpace(hostOptions?.ContentRoot)) dictionary.Add("contentRoot", hostOptions.ApplicationName);
                    if (dictionary.Any()) config.AddInMemoryCollection(dictionary);
                });

                builder.Properties.Add("workers", hostOptions.Workers ??= new List<string>());

                builder.ConfigureAppConfiguration(config =>
                {
                    if (appOptions?.Options != null && appOptions.Options.Any())
                    {
                        var options = appOptions.Options
                            .Select(o => o.Split('=', StringSplitOptions.RemoveEmptyEntries))
                            .Where(pair => pair.Length == 2)
                            .Select(pair => new KeyValuePair<string, string>(pair[0], pair[1]));

                        config.AddInMemoryCollection(options);
                    }
                });
                var host = await buildHost(builder);
                await host.StartAsync();
            });
        }
    }

    internal class HostOptions
    {
        public string ApplicationName { get; set; }
        public string Environment { get; set; }
        public string ContentRoot { get; set; }
        public List<string> Workers { get; set; }
    }

    internal class AppOptions
    {
        public List<string> Options { get; set; }
    }
}
