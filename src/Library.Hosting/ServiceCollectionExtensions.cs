using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.OpenGenerics;
using Library.Dataflow;
using Library.Platform.Queuing;
using Microsoft.Extensions.DependencyInjection;
using NamedParameter = Autofac.NamedParameter;

namespace Library.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterGenericQueueMessageWorkerFactory(this ContainerBuilder builder)
        {
            builder.RegisterGeneric((_, t, _) =>
                {
                    var type = typeof(BufferBlock<>);
                    var generic = type.MakeGenericType(t);
                    var instance = Activator.CreateInstance(generic);
                    return instance;
                })
                .IfNotRegistered(typeof(ITargetBlock<>))
                .IfNotRegistered(typeof(ISourceBlock<>))
                .SingleInstance()
                .As(typeof(ITargetBlock<>))
                .As(typeof(ISourceBlock<>));
            
            builder.RegisterGeneric((c, t, p) =>
                {
                    var client = c.Resolve<IQueueClient>(p);
                    if (client == null) throw new NotImplementedException("To resolve a QueueMessageWorker<>, an IQueueClient registration must exist that accepts a named parameter with the name \"queue\" that specifies the message queue to work with.");

                    var buffer = c.Resolve(typeof(ITargetBlock<>).MakeGenericType(t));
                    var type = typeof(QueueMessageProducer<>);
                    var generic = type.MakeGenericType(t);
                    var producer = Activator.CreateInstance(generic, client, buffer, 1, default);

                    return producer;
                })
                .IfNotRegistered(typeof(QueueMessageProducer<>))
                .SingleInstance()
                .As(typeof(QueueMessageProducer<>));

            builder.RegisterGeneric((c, t, p) =>
                {
                    // Get queue parameter
                    var parameters = p.ToList();
                    var queue = parameters.FirstOrDefault(np => np is NamedParameter {Name: "queue"}) as NamedParameter;
                    if (queue == null) throw new ArgumentNullException("parameters", "To resolve a queue message worker, the queue name must be supplied as a named parameter with the name \"queue\".");

                    // Resolve producer
                    var openProducerType = typeof(QueueMessageProducer<>);
                    var genericProducerType = openProducerType.MakeGenericType(t);
                    var producer = c.Resolve(genericProducerType, queue);

                    // Resolve consumer
                    var openConsumerType = typeof(MessageConsumer<>);
                    var genericConsumerType = openConsumerType.MakeGenericType(t);
                    var consumer = c.Resolve(genericConsumerType);

                    // Create worker
                    var openWorkerType = typeof(QueueMessageWorker<>);
                    var genericWorkerType = openWorkerType.MakeGenericType(t);
                    var worker = Activator.CreateInstance(genericWorkerType, producer, consumer, default);

                    // Set name of worker
                    var name = (p.FirstOrDefault(param => param is NamedParameter np && np.Name == "name") as NamedParameter)?.Value as string;
                    ((INamedBackgroundService) worker).Name = name;

                    return worker;
                })
                .IfNotRegistered(typeof(QueueMessageWorker<>))
                .As(typeof(QueueMessageWorker<>));
        }
    }
}
