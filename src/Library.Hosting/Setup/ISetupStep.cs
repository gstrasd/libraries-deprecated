using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Hosting.Setup
{
    public interface ISetupStep
    {
        string Name { get; }
        Task ExecuteAsync();
    }
}