using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// Configuration options for the schedule.
    /// </summary>
    public class ScheduleOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostBuilder"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public ScheduleOptions(IServiceCollection services) => Services = services;
        /// <summary>
        /// The cron expression
        /// </summary>
        public string CronExpression { get; set; }

        /// <summary>
        /// The job description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The job group
        /// </summary>
        public string Group { get; set; }
        internal IServiceCollection Services { get; }
    }
}
