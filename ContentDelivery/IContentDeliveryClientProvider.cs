using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libraries.ContentDelivery
{
    public interface IContentDeliveryClientProvider
    {
        IContentDeliveryClient Build();
    }
}