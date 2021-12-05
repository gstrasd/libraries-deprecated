using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Library.Dataflow;

namespace Library.Messages.Social
{
    public class ProcessInstagramAccountMessage : IMessage
    {
        public Guid CorrelationId { get; set; }
        public string ProviderId { get; set; }
        public string InstagramAccount { get; set; }
    }
}
