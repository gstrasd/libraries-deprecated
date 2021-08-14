using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Configuration;

namespace Library.Serilog
{
    public class ConfigurationLoggerSettings : ILoggerSettings
    {
        private readonly IConfiguration _configuration;
        internal readonly bool LogCorrelationId;

        public ConfigurationLoggerSettings(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _configuration = configuration;
            Boolean.TryParse(configuration["Serilog:LogCorrelationId"], out LogCorrelationId);
        }

        public void Configure(LoggerConfiguration loggerConfiguration)
        {
            loggerConfiguration.ReadFrom.Configuration(_configuration);
        }
    }
}