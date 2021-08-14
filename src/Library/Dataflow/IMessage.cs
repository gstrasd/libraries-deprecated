using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Dataflow
{
    public interface IMessage
    {
        Guid CorrelationId { get; set; }
    }
}