using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Library.Hosting.AspNetCore.Middleware;
using Library.Serilog.Enrichers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Library.Serilog
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLogLevelSwitch(this IServiceCollection services, LogEventLevel level)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return services.AddSingleton(p => new LoggingLevelSwitch(level));
        }

        public static IServiceCollection AddSerilog(this IServiceCollection services, Action<SerilogOptions> setupAction)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));

            services.AddSingleton<ILogger, Logger>(p =>
            {
                var configuration = new LoggerConfiguration();
                var options = new SerilogOptions();
                
                // Configure logger
                setupAction(options);
                options.Settings?.Configure(configuration);

                // Establish level switch
                var @switch = p.GetService<LoggingLevelSwitch>();
                if (@switch != default)
                {
                    configuration.MinimumLevel.ControlledBy(@switch);
                }

                // Set up correlation id logging
                if (options.LogCorrelationId || options.Settings is ConfigurationLoggerSettings settings && settings.LogCorrelationId)
                {
                    services.AddHttpContextAccessor();
                    services.AddSingleton<AsyncLocal<Guid>>();
                    var enricher = new CorrelationIdEnricher(p.GetService<IHttpContextAccessor>(), p.GetService<AsyncLocal<Guid>>()) { Enabled = true };
                    configuration.Enrich.With(enricher);
                }

                // Add contextual logging support
                configuration.Enrich.FromLogContext();

                var logger = configuration.CreateLogger();
                return logger;
            });

            return services;
        }
    }
}