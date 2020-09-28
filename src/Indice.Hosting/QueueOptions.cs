using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// Configuration options for the queue.
    /// </summary>
    public class QueueOptions
    {
        internal IServiceCollection Services;
        /// <summary>
        /// The name of the queue. If not specified a random GUID is assigned.
        /// </summary>
        public string QueueName { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Specifies the time interval between two attempts to dequeue new items. Defaults to 15 seconds.
        /// </summary>
        public int PollingIntervalInSeconds { get; set; } = 15;
    }
}
