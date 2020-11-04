using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure the worker host.
    /// </summary>
    public class WorkerHostBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public WorkerHostBuilder(IServiceCollection services) : this(services, null) { }

        internal WorkerHostBuilder(IServiceCollection services, Type queueType) {
            Services = services;
            QueueType = queueType;
        }

        internal IServiceCollection Services { get; }
        internal Type QueueType { get; }
    }

    /// <summary>
    /// A helper class to configure the worker host. This variation enables the just added queue triggered job to change its qmessage store
    /// </summary>
    public class WorkerHostBuilderForQueue : WorkerHostBuilder
    {
        internal WorkerHostBuilderForQueue(IServiceCollection services, Type queueType, Type workItemType) : base(services, queueType) {
            WorkItemType = workItemType;
        }

        internal Type WorkItemType { get; }
    }
}
