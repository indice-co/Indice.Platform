using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting.Quartz
{
    /// <summary>
    /// An "intermediary" <see cref="IJob"/> implementation, <see cref="QuartzJobRunner"/>, that sits between the <seealso cref="IJobFactory"/> and the <seealso cref="IJob "/> you want to run. 
    /// It takes care of instanciating your real jobs while managing scope.
    /// </summary>
    public class QuartzJobRunner : IJob
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Constructs the <see cref="QuartzJobRunner"/>
        /// </summary>
        /// <param name="serviceProvider"></param>
        public QuartzJobRunner(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Called by the <see cref="IScheduler"/> when a <see cref="ITrigger"/> fires that is associated
        /// with the <see cref="IJob"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Execute(IJobExecutionContext context) {
            using (var scope = _serviceProvider.CreateScope()) {
                var jobType = context.JobDetail.JobType;
                var job = scope.ServiceProvider.GetRequiredService(jobType) as IJob;

                await job.Execute(context);
            }
        }
    }
}
