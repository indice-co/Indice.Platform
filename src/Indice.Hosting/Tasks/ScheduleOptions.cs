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
        /// The cron expression.
        /// </summary>
        public string CronExpression { get; set; }
        /// <summary>
        /// Job name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The job description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The job group.
        /// </summary>
        public string Group { get; set; }
        /// <summary>
        /// When true it ensures the operation will run only in one instance at a time. Even when deployed in multiple machines. 
        /// </summary>
        /// <remarks>Under the hood will utilize an <see cref="Indice.Services.ILockManager"/> in order to block other competing worker instances from executing. 
        /// </remarks>
        public bool Singleton { get; set; }
        internal IServiceCollection Services { get; }
    }
}
