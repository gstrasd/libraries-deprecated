using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Libraries.ContentDelivery
{
    public static class ContentDeliveryClientBuilderExtensions
    {
        public static ContentDeliveryClientBuilder UseConfiguration(this ContentDeliveryClientBuilder builder, IConfiguration configuration)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            ((IContentDeliveryClientBuilder)builder).Configuration = configuration;
            return builder;
        }
    }
}