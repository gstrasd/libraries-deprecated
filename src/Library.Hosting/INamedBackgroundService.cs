using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Hosting
{
    public interface INamedBackgroundService
    {
        string Name { get; set; }
    }
}