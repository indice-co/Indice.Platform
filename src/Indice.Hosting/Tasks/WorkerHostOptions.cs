using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// Options for configuring the worker host.
    /// </summary>
    public class WorkerHostOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostOptions"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public WorkerHostOptions(IServiceCollection services) : this(services, null) { }

        internal WorkerHostOptions(IServiceCollection services, Type workItemQueueType) {
            Services = services;
            WorkItemQueueType = workItemQueueType;
        }

        internal IServiceCollection Services { get; }
        internal Type WorkItemQueueType { get; set; }
    }
}
