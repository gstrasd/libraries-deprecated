using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.OpenGenerics;
using Library.Configuration;
using Library.Dataflow;
using Library.Platform.Queuing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NamedParameter = Autofac.NamedParameter;

namespace Library.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static void RegisterQueueMessageProducers(this ContainerBuilder builder)
        {
            builder.RegisterGeneric((c, t) =>
                {
                    // Resolve queue client
                    var client = c.Resolve<IQueueClient>();

                    // Resolve buffer
                    var openBufferType = typeof(ITargetBlock<>);
                    var genericBufferType = openBufferType.MakeGenericType(t);
                    var buffer = c.Resolve(genericBufferType);

                    // Create producer
                    var openProducerType = typeof(QueueMessageProducer<>);
                    var genericProducerType = openProducerType.MakeGenericType(t);
                    var producer = Activator.CreateInstance(genericProducerType, client, buffer);

                    return producer;
                })
                .IfNotRegistered(typeof(MessageProducer<>))
                .SingleInstance()
                .As(typeof(MessageProducer<>));
        }

        public static void RegisterMessageWorkers(this ContainerBuilder builder, IEnumerable<MessageWorkerConfiguration> configuration)
        {
            foreach (var config in configuration)
            {
                if (!config.Enabled) return;
                
                var messageType = Type.GetType(config.MessageType);
                if (messageType == null) return;

                builder.Register(c =>
                    {
                        var openType = typeof(MessageWorker<>);
                        var closedType = openType.MakeGenericType(messageType);
                        var worker = c.Resolve(closedType, new NamedParameter("name", config.Name));

                        return worker;
                    })
                    .SingleInstance()
                    .As<IBackgroundService>()
                    .As<IHostedService>();
            }

            builder.RegisterGeneric((_, t) =>
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
                    // Resolve producer
                    var openProducerType = typeof(MessageProducer<>);
                    var genericProducerType = openProducerType.MakeGenericType(t);
                    var producer = c.Resolve(genericProducerType);

                    // Resolve consumer
                    var openConsumerType = typeof(MessageConsumer<>);
                    var genericConsumerType = openConsumerType.MakeGenericType(t);
                    var consumer = c.Resolve(genericConsumerType);

                    // Create worker
                    var openWorkerType = typeof(MessageWorker<>);
                    var genericWorkerType = openWorkerType.MakeGenericType(t);
                    var name = (p.FirstOrDefault(param => param is NamedParameter { Name: "name" }) as NamedParameter)?.Value as string;
                    var worker = Activator.CreateInstance(genericWorkerType, producer, consumer, name);

                    return worker;
                })
                .IfNotRegistered(typeof(MessageWorker<>))
                .As(typeof(MessageWorker<>));
        }
    }
}
