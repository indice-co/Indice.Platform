using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting
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
        public JobFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        /// <inheritdoc />
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => _serviceProvider.GetRequiredService<QuartzJobRunner>();

        /// <inheritdoc />
        public void ReturnJob(IJob job) {
            // We let the DI container handle this
        }
    }
}
