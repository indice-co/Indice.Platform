using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting.Quartz
{
    /// <summary>
    /// Defines methods for objects that are managed by the host.
    /// </summary>
    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IEnumerable<JobSchedule> _jobSchedules;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="schedulerFactory"></param>
        /// <param name="jobSchedules"></param>
        /// <param name="jobFactory"></param>
        public QuartzHostedService(
            ISchedulerFactory schedulerFactory,
            IEnumerable<JobSchedule> jobSchedules,
            IJobFactory jobFactory) {
            _schedulerFactory = schedulerFactory;
            _jobSchedules = jobSchedules;
            _jobFactory = jobFactory;
        }

        /// <summary>
        /// the Quartz <see cref="IScheduler"/>
        /// </summary>
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken cancellationToken) {
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;

            foreach (var jobSchedule in _jobSchedules) {
                var job = CreateJob(jobSchedule);
                var trigger = CreateTrigger(jobSchedule);

                await Scheduler.ScheduleJob(job, trigger, cancellationToken);
            }

            await Scheduler.Start(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken) {
            await Scheduler?.Shutdown(cancellationToken);
        }

        private static ITrigger CreateTrigger(JobSchedule schedule) {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{schedule.JobType.FullName}.trigger")
                .WithCronSchedule(schedule.CronExpression)
                .WithDescription(schedule.CronExpression)
                .Build();
        }

        private static IJobDetail CreateJob(JobSchedule schedule) {
            var jobType = schedule.JobType;
            return JobBuilder
                .Create(jobType)
                .WithIdentity(jobType.FullName)
                .WithDescription(jobType.Name)
                .Build();
        }
    }
}
