using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// A helper class to configure the way that a job is triggered.
    /// </summary>
    public class TaskTriggerBuilder
    {
        /// <summary>
        /// Creates a new instance of <see cref="TaskTriggerBuilder"/>.
        /// </summary>
        /// <param name="services"></param>
        public TaskTriggerBuilder(IServiceCollection services) : this(services, null, null) { }

        internal TaskTriggerBuilder(IServiceCollection services, Type jobHandlerType, Type queueType) {
            Services = services;
            JobHandlerType = jobHandlerType;
            QueueType = queueType;
        }

        internal IServiceCollection Services { get; }
        internal Type JobHandlerType { get; }
        internal Type QueueType { get; }
    }
}
