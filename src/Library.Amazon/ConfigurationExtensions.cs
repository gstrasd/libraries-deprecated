using System;
using System.Collections.Generic;
using System.Text;
using  Library.Configuration;
using Microsoft.Extensions.Configuration;

namespace Library.Amazon
{
    public static class ConfigurationExtensions
    {
        public static List<SqsQueueConfiguration> GetSqsQueueClientConfiguration(this IConfiguration configuration, string key)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            return configuration.Bind<List<SqsQueueConfiguration>>(key);
        }
    }
}
