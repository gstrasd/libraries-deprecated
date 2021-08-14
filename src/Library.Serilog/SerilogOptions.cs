using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Configuration;

namespace Library.Serilog
{
    public class SerilogOptions
    {
        public ILoggerSettings Settings { get; set; }
        public bool LogCorrelationId { get; set; }
    }
}