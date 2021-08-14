using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Installation
{
    public interface IInstaller
    {
        Task ExecuteAsync();
    }
}