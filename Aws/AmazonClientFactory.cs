using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS;
using Amazon.SQS.Model;
using Libraries.Messaging;

namespace Libraries.Aws
{
    public class AmazonClientFactory : IQueueClientFactory
    {
        private readonly AWSOptions _options;
        private readonly Action<Message> _messageReceived;
        private readonly Action<Message> _messageDeleted;
        private readonly int _receiveWaitTimeSeconds;
        private readonly int _receiveVisibilityTimeout;

        public AmazonClientFactory(
            AWSOptions options, 
            int receiveWaitTimeSeconds = 5, int receiveVisibilityTimeout = 10,
            Action<Message> messageReceived = null, Action<Message> messageDeleted = null)
        {
            _options = options;
            _messageReceived = messageReceived;
            _messageDeleted = messageDeleted;
            _receiveWaitTimeSeconds = receiveWaitTimeSeconds;
            _receiveVisibilityTimeout = receiveVisibilityTimeout;
        }

        public IQueueClient CreateQueueClient(string queueName)
        {
            var sqsClient = _options.CreateServiceClient<IAmazonSQS>();
            var client = new AmazonSqsQueueClient(sqsClient, queueName, _receiveWaitTimeSeconds, _receiveVisibilityTimeout);
            if (_messageReceived != null) client.MessageReceived += _messageReceived;
            if (_messageDeleted != null) client.MessageDeleted += _messageDeleted;

            return client;
        }
    }
}