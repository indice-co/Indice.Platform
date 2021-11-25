using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tasks
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

        internal WorkerHostBuilder(IServiceCollection services, WorkerHostOptions options) {
            Services = services;
            Options = options;
        }

        internal IServiceCollection Services { get; }
        internal WorkerHostOptions Options { get; }
    }

    /// <summary>
    /// A helper class to configure the worker host. This variation enables the just added queue triggered job to change its qmessage store
    /// </summary>
    public class WorkerHostBuilderForQueue : WorkerHostBuilder
    {
        internal WorkerHostBuilderForQueue(IServiceCollection services, WorkerHostOptions options, Type workItemType) : base(services, options) {
            WorkItemType = workItemType;
        }

        internal Type WorkItemType { get; }
    }
}
