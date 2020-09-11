using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting.Quartz
{
    /// <summary>
    /// The <see cref="IJobFactory"/> implementation that uses the DI to construct <seealso cref="IJob"/> instances.
    /// </summary>
    public class JobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs the <see cref="JobFactory"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public JobFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Called by the scheduler at the time of the trigger firing, in order to produce a <see cref="IJob"/> instance on which to call Execute.
        /// </summary>
        /// <param name="bundle">The <see cref="TriggerFiredBundle"/> from which the <see cref="IJobDetail"/> and other info relating to the trigger firing can be obtained.</param>
        /// <param name="scheduler">A handle to the scheduler that is about to execute the job.</param>
        /// <returns>The newly instantiated job.</returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => _serviceProvider.GetRequiredService<QuartzJobRunner>();

        /// <summary>
        /// Allows the job factory to destroy/cleanup the job if needed.
        /// </summary>
        /// <param name="job"></param>
        public void ReturnJob(IJob job) { }
    }
}
