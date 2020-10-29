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
        /// Specifies the time interval between two attempts to dequeue new items. Defaults to 15 seconds.
        /// </summary>
        public int PollingIntervalInSeconds { get; set; } = 15;
        internal IServiceCollection Services { get; }
    }
}
