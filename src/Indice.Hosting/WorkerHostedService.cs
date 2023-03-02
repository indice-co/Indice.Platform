using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Indice.Hosting.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting
{
    /// <summary>
    /// A hosted service that manages the lifetime of configured background tasks.
    /// </summary>
    internal class WorkerHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly ILogger<WorkerHostedService> _logger;
        private readonly IEnumerable<DequeueJobSettings> _dequeueJobSchedules;
        private readonly IEnumerable<ScheduledJobSettings> _scheduledJobSettings;
        private readonly IJobFactory _jobFactory;

        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostedService"/>.
        /// </summary>
        /// <param name="schedulerFactory">Provides a mechanism for obtaining client-usable handles to <see cref="IScheduler"/> instances.</param>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="dequeueJobSettings">Contains meta-data about the <see cref="DequeueJob{TWorkItem}"/> instances that have been configured.</param>
        /// <param name="scheduledJobSettings">Job schedule settings. Describes what to execute and when.</param>
        /// <param name="jobFactory">A JobFactory is responsible for producing instances of <see cref="IJob"/> classes.</param>
        public WorkerHostedService(ISchedulerFactory schedulerFactory, ILogger<WorkerHostedService> logger, IEnumerable<DequeueJobSettings> dequeueJobSettings, IEnumerable<ScheduledJobSettings> scheduledJobSettings, IJobFactory jobFactory) {
            _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dequeueJobSchedules = dequeueJobSettings ?? throw new ArgumentNullException(nameof(dequeueJobSettings));
            _scheduledJobSettings = scheduledJobSettings ?? throw new ArgumentNullException(nameof(scheduledJobSettings));
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
            foreach (var dequeueJobSchedule in _dequeueJobSchedules) {
                var dequeueJob = JobBuilder.Create(typeof(DequeueJob<>).MakeGenericType(dequeueJobSchedule.WorkItemType))
                                           .StoreDurably()
                                           .WithIdentity(name: dequeueJobSchedule.Name, group: JobGroups.InternalJobsGroup)
                                           .SetJobData(new JobDataMap(new Dictionary<string, object> {
                                               [JobDataKeys.QueueName] = dequeueJobSchedule.Name,
                                               [JobDataKeys.PollingInterval] = dequeueJobSchedule.PollingInterval,
                                               [JobDataKeys.MaxPollingInterval] = dequeueJobSchedule.MaxPollingInterval,
                                               [JobDataKeys.JobHandlerType] = dequeueJobSchedule.JobHandlerType
                                           } as IDictionary<string, object>))
                                           .Build();
                await Scheduler.AddJob(dequeueJob, replace: true, cancellationToken);
                for (var i = 1; i <= dequeueJobSchedule.InstanceCount; i++) {
                    var jobTrigger = TriggerBuilder.Create()
                                                   .ForJob(dequeueJob)
                                                   .WithIdentity(name: $"{dequeueJobSchedule.Name}{TriggerNames.DequeueJobTrigger}{i}", group: JobGroups.InternalJobsGroup)
                                                   .StartNow()
                                                   .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMilliseconds(dequeueJobSchedule.PollingInterval + (dequeueJobSchedule.PollingInterval / dequeueJobSchedule.InstanceCount * (i - 1)))).RepeatForever())
                                                   .Build();
                    await Scheduler.ScheduleJob(jobTrigger, cancellationToken);
                }
                if (dequeueJobSchedule.CleanupInterval <= 0) {
                    continue;
                }
                var cleanUpJob = JobBuilder.Create(typeof(DequeuedCleanupJob<>).MakeGenericType(dequeueJobSchedule.WorkItemType))
                                           .StoreDurably()
                                           .WithIdentity(name: $"{dequeueJobSchedule.Name}CleanUp", group: JobGroups.InternalJobsGroup)
                                           .SetJobData(new JobDataMap(new Dictionary<string, object> {
                                               [JobDataKeys.QueueName] = dequeueJobSchedule.Name,
                                               [JobDataKeys.CleanUpBatchSize] = dequeueJobSchedule.CleanupBatchSize,
                                           } as IDictionary<string, object>))
                                           .Build();
                await Scheduler.AddJob(cleanUpJob, replace: true, cancellationToken);
                var cleanUpTrigger = TriggerBuilder.Create()
                                                   .ForJob(cleanUpJob)
                                                   .WithIdentity(name: $"{cleanUpJob.Key.Name}Trigger", group: JobGroups.InternalJobsGroup)
                                                   .StartNow()
                                                   .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromSeconds(dequeueJobSchedule.CleanupInterval)).RepeatForever())
                                                   .Build();
                await Scheduler.ScheduleJob(cleanUpTrigger, cancellationToken);
            }
            foreach (var schedule in _scheduledJobSettings) {
                var jobDetails = JobBuilder.Create(typeof(ScheduledJob<,>).MakeGenericType(schedule.JobHandlerType, schedule.JobStateType))
                                           .StoreDurably()
                                           .WithIdentity(name: schedule.Name, group: schedule.Group ?? JobGroups.InternalJobsGroup)
                                           .WithDescription(schedule.Description)
                                           .SetJobData(new JobDataMap(new Dictionary<string, object> {
                                               [JobDataKeys.JobHandlerType] = schedule.JobHandlerType,
                                               [JobDataKeys.Singleton] = schedule.Singleton
                                           } as IDictionary<string, object>))
                                           .Build();
                await Scheduler.AddJob(jobDetails, replace: true, cancellationToken);
                var jobTrigger = TriggerBuilder.Create()
                                               .ForJob(jobDetails)
                                               .WithIdentity(name: $"{schedule.Name}.trigger", group: schedule.Group ?? JobGroups.InternalJobsGroup)
                                               .StartNow()
                                               .WithCronSchedule(schedule.CronExpression)
                                               .WithDescription(schedule.CronExpression)
                                               .Build();
                await Scheduler.ScheduleJob(jobTrigger, cancellationToken);
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
