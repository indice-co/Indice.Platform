using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// Configuration options for the queue.
    /// </summary>
    public class QueueOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public QueueOptions(IServiceCollection services) => Services = services;

        /// <summary>
        /// The name of the queue. If not specified a random GUID is assigned.
        /// </summary>
        public string QueueName { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Specifies the time interval between two attempts to dequeue new items. Defaults to 300 milliseconds.
        /// </summary>
        public int PollingInterval { get; set; } = 300;
        /// <summary>
        /// Specifies the maximum time interval between two attempts to dequeue new items. Used as a backoff strategy threshold. Defaults to 300 milliseconds.
        /// </summary>
        public int MaxPollingInterval { get; set; } = 5000;
        /// <summary>
        /// Specifies number of concurrent instances. Defaults to one.
        /// </summary>
        public int InstanceCount { get; set; } = 1;
        internal IServiceCollection Services { get; }
    }
}
