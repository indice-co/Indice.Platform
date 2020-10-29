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

        internal WorkerHostBuilder(IServiceCollection services, Type workItemQueueType) {
            Services = services;
            WorkItemQueueType = workItemQueueType;
        }

        internal IServiceCollection Services { get; }
        internal Type WorkItemQueueType { get; }
    }
}
