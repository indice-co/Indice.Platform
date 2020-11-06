using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting
{
    /// <summary>
    /// The <see cref="IJobFactory"/> implementation that uses the DI to construct <seealso cref="IJob"/> instances.
    /// </summary>
    internal class QuartzJobFactory : IJobFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs the <see cref="QuartzJobFactory"/>.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public QuartzJobFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        /// <inheritdoc />
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler) => _serviceProvider.GetRequiredService<QuartzJobRunner>();

        /// <inheritdoc />
        public void ReturnJob(IJob job) { /* We let the DI container handle this.*/ }
    }
}
