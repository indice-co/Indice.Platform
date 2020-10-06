using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure a job trigger.
    /// </summary>
    public class JobTriggerBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="JobTriggerBuilder"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="jobHandlerType"></param>
        public JobTriggerBuilder(IServiceCollection services, Type jobHandlerType) {
            Services = services;
            JobHandlerType = JobHandlerType;
        }

        /// <summary>
        /// Specifies the contract for a collection of service descriptors.
        /// </summary>
        public IServiceCollection Services { get; }
        /// <summary>
        /// The type of the job handler.
        /// </summary>
        public Type JobHandlerType { get; }
    }
}
