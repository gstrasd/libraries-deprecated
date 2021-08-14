using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Library.Hosting.AspNetCore
{
    public interface ISetupWebHost : ISetupHost
    {
        void ConfigureStartup(IWebHostEnvironment environment, IApplicationBuilder builder) { }
    }
}
