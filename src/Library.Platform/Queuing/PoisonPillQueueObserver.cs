using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Platform.Queuing
{
    public class PoisonPillQueueObserver : IObserver<string>
    {
        private readonly IQueueClient _client;

        public PoisonPillQueueObserver(IQueueClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            _client = client;
        }

        public void OnCompleted()
        {
            // NOOP
        }

        public void OnError(Exception error)
        {
            if (error is QueueClientReadMessageException exception)
            {
                // TODO: verify that this runs on another thread and that execution
                // TODO: leaves this method while the WriteRawMessageAsync is still running
                _client.WriteRawMessageAsync(exception.RawMessage).Start();
            }
        }

        public void OnNext(string value)
        {
            // NOOP
        }
    }
}
