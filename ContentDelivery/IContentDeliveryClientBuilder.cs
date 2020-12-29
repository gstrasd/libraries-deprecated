using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Libraries.ContentDelivery
{
    public interface IContentDeliveryClientBuilder
    {
        IConfiguration Configuration { get; set; }
        IContentDeliveryClientProvider Provider { get; set; }
    }
}