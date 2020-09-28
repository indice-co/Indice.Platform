using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class QueuedHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IEnumerable<DequeueJobSchedule> _dequeueJobSchedules;
        private readonly IJobFactory _jobFactory;

        /// <summary>
        /// Creates a new instance of <see cref="QueuedHostedService"/>.
        /// </summary>
        /// <param name="schedulerFactory">rovides a mechanism for obtaining client-usable handles to <see cref="IScheduler"/> instances.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="dequeueJobSchedules">Contains medata about the <see cref="DequeueJob{TWorkItem}"/> instances that have been configured.</param>
        /// <param name="jobFactory">A JobFactory is responsible for producing instances of <see cref="IJob"/> classes.</param>
        public QueuedHostedService(ISchedulerFactory schedulerFactory, ILogger<QueuedHostedService> logger, IEnumerable<DequeueJobSchedule> dequeueJobSchedules, IJobFactory jobFactory) {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dequeueJobSchedules = dequeueJobSchedules ?? throw new ArgumentNullException(nameof(dequeueJobSchedules));
            _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        }

        /// <summary>
        /// The Quartz scheduler.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is running.");
            Scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            Scheduler.JobFactory = _jobFactory;
            foreach (var job in _dequeueJobSchedules) {
                var jobDetails = JobBuilder.Create(typeof(DequeueJob<>).MakeGenericType(job.WorkItemType))
                                           .WithIdentity(name: job.Name, group: JobGroups.InternalJobsGroup)
                                           .Build();
                var jobTrigger = TriggerBuilder.Create()
                                               .WithIdentity(name: TriggerNames.DequeueJobTrigger, group: JobGroups.InternalJobsGroup)
                                               .StartNow()
                                               .WithSimpleSchedule(x => x.WithIntervalInSeconds(job.PollingIntervalInSeconds).RepeatForever())
                                               .Build();
                await Scheduler.ScheduleJob(jobDetails, jobTrigger);
            }
            await Scheduler.Start(cancellationToken);
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken) {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            await Scheduler?.Shutdown(cancellationToken);
        }
    }
}
