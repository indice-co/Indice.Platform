using System;
using System.Text.Json;
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
        public WorkerHostOptions(IServiceCollection services) {
            Services = services;
            JsonOptions = new WorkerJsonOptions();
        }

        internal IServiceCollection Services { get; }
        internal Type QueueStoreType { get; set; }
        internal Type ScheduledTaskStoreType { get; set; }
        internal Type LockStoreType { get; set; }

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> used internally whenever a payload needs to be persisted. 
        /// </summary>
        public WorkerJsonOptions JsonOptions { get; }
    }
}
