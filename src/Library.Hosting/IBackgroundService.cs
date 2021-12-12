using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Hosting
{
    public interface IBackgroundService
    {
        string Name { get; }
        event Action<IBackgroundService> OnStart;
        event Action<IBackgroundService> OnStop;
    }
}