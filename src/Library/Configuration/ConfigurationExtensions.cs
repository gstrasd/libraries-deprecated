using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Library.Configuration
{
    public static class ConfigurationExtensions
    {
        public static T Bind<T>(this IConfiguration configuration, string key) where T: new()
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var section = configuration.GetSection(key);
            var instance = new T();
            section.Bind(instance);

            return instance;
        }

        public static T Bind<T>(this IConfiguration configuration) where T : new()
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var instance = new T();
            configuration.Bind(instance);

            return instance;
        }
    }
}
