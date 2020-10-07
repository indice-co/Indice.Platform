using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure the way that a job is triggered.
    /// </summary>
    public class JobTriggerBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="JobTriggerBuilder"/>.
        /// </summary>
        /// <param name="services"></param>
        public JobTriggerBuilder(IServiceCollection services) : this(services, null, null) { }

        internal JobTriggerBuilder(IServiceCollection services, Type jobHandlerType, Type workItemQueueType) {
            Services = services;
            JobHandlerType = jobHandlerType;
            WorkItemQueueType = workItemQueueType;
        }

        internal IServiceCollection Services { get; }
        internal Type JobHandlerType { get; }
        internal Type WorkItemQueueType { get; }
    }
}
