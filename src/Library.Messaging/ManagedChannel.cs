using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Library.Messaging
{
    public class ManagedChannel<TMessage> : Channel<TMessage>
    {
        protected Channel<TMessage> Channel { get; }

        public ManagedChannel(int capacity)
        {
            Capacity = capacity;
            Channel = System.Threading.Channels.Channel.CreateBounded<TMessage>(capacity);
            Reader = Channel.Reader;
            Writer = Channel.Writer;
        }

        public int Capacity { get; }

        public int Count => Reader.Count;
    }
}
