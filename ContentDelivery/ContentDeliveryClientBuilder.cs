using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.ContentDelivery
{
    public class ContentDeliveryClientBuilder : IContentDeliveryClientBuilder
    {
        IConfiguration IContentDeliveryClientBuilder.Configuration { get; set; }
        IContentDeliveryClientProvider IContentDeliveryClientBuilder.Provider { get; set; }

        public IContentDeliveryClient Build()
        {
            var provider = ((IContentDeliveryClientBuilder)this).Provider;
            if (provider == null) throw new InvalidOperationException("No content delivery client provider was specified.");

            return provider.Build();
        }
    }
}
