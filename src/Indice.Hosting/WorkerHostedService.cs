using Indice.Hosting.Services;
using Indice.Hosting.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Spi;

namespace Indice.Hosting;

/// <summary>A hosted service that manages the lifetime of configured background tasks.</summary>
internal class WorkerHostedService : IHostedService
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<WorkerHostedService> _logger;
    private readonly IEnumerable<DequeueJobSettings> _dequeueJobSchedules;
    private readonly IEnumerable<ScheduledJobSettings> _scheduledJobSettings;
    private readonly IJobFactory _jobFactory;
    private readonly WorkerLifetimeOptions _workerLifetimeOptions;
    private readonly RunNowSettings? _runNowSettings;
    private readonly WorkerServerRegistry? _serverRegistry;

    /// <summary>Creates a new instance of <see cref="WorkerHostedService"/>.</summary>
    /// <param name="schedulerFactory">Provides a mechanism for obtaining client-usable handles to <see cref="IScheduler"/> instances.</param>
    /// <param name="logger">Represents a type used to perform logging.</param>
    /// <param name="dequeueJobSettings">Contains meta-data about the <see cref="DequeueJob{TWorkItem}"/> instances that have been configured.</param>
    /// <param name="scheduledJobSettings">Job schedule settings. Describes what to execute and when.</param>
    /// <param name="jobFactory">A JobFactory is responsible for producing instances of <see cref="IJob"/> classes.</param>
    /// <param name="workerLifetimeOptions">The configured WorkerLifetimeOptions instance.</param>
    /// <param name="runNowSettings">The settings of the run now job. Empty when run now is not supported or disabled.</param>
    /// <param name="serverRegistries">Registers this worker in the server table. Empty when the server registration is not supported or disabled.</param>
    public WorkerHostedService(ISchedulerFactory schedulerFactory, ILogger<WorkerHostedService> logger, IEnumerable<DequeueJobSettings> dequeueJobSettings, IEnumerable<ScheduledJobSettings> scheduledJobSettings, IJobFactory jobFactory, IOptions<WorkerLifetimeOptions> workerLifetimeOptions, IEnumerable<RunNowSettings> runNowSettings, IEnumerable<WorkerServerRegistry> serverRegistries) {
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dequeueJobSchedules = dequeueJobSettings ?? throw new ArgumentNullException(nameof(dequeueJobSettings));
        _scheduledJobSettings = scheduledJobSettings ?? throw new ArgumentNullException(nameof(scheduledJobSettings));
        _jobFactory = jobFactory ?? throw new ArgumentNullException(nameof(jobFactory));
        _workerLifetimeOptions = workerLifetimeOptions?.Value ?? throw new ArgumentNullException(nameof(workerLifetimeOptions));
        _runNowSettings = runNowSettings?.LastOrDefault();
        _serverRegistry = serverRegistries?.LastOrDefault();
    }

    /// <summary>The Quartz scheduler.</summary>
    public IScheduler? Scheduler { get; private set; }

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
                                           [JobDataKeys.JobHandlerType] = dequeueJobSchedule.JobHandlerType,
                                           [JobDataKeys.MaxRetryCount] = dequeueJobSchedule.MaxRetryCount
                                       } as IDictionary<string, object>))
                                       .Build();
            await Scheduler.AddJob(dequeueJob, replace: true, cancellationToken);
            for (var i = 1; i <= dequeueJobSchedule.InstanceCount; i++) {
                var jobTrigger = TriggerBuilder.Create()
                                               .ForJob(dequeueJob)
                                               .WithIdentity(name: $"{dequeueJobSchedule.Name}{TriggerNames.DequeueJobTrigger}{i}", group: JobGroups.InternalJobsGroup)
                                               .StartNow()
                                               .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMilliseconds(dequeueJobSchedule.PollingInterval + (dequeueJobSchedule.PollingInterval / dequeueJobSchedule.InstanceCount * (i - 1))))
                                                                         .RepeatForever())
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
        await ScheduleRunNowJob(Scheduler, cancellationToken);
        await ScheduleServerHeartbeatJob(Scheduler, cancellationToken);
        await Scheduler.Start(cancellationToken);
    }

    private async Task ScheduleServerHeartbeatJob(IScheduler scheduler, CancellationToken cancellationToken) {
        if (_serverRegistry is null) {
            return;
        }
        // Failures are logged inside and retried by the heartbeat, so a database without the server tables never stops the worker.
        await _serverRegistry.Register(cancellationToken);
        var heartbeatJob = JobBuilder.Create<ServerHeartbeatJob>()
                                     .StoreDurably()
                                     .WithIdentity(name: nameof(ServerHeartbeatJob), group: JobGroups.InternalJobsGroup)
                                     .Build();
        await scheduler.AddJob(heartbeatJob, replace: true, cancellationToken);
        var heartbeatTrigger = TriggerBuilder.Create()
                                             .ForJob(heartbeatJob)
                                             .WithIdentity(name: $"{nameof(ServerHeartbeatJob)}Trigger", group: JobGroups.InternalJobsGroup)
                                             .StartAt(DateTimeOffset.UtcNow + _serverRegistry.HeartbeatInterval)
                                             .WithSimpleSchedule(x => x.WithInterval(_serverRegistry.HeartbeatInterval).RepeatForever())
                                             .Build();
        await scheduler.ScheduleJob(heartbeatTrigger, cancellationToken);
    }

    private async Task ScheduleRunNowJob(IScheduler scheduler, CancellationToken cancellationToken) {
        if (_runNowSettings is null) {
            return;
        }
        // Only the tasks hosted by this worker, so a worker never claims a request it cannot serve.
        var jobKeys = new Dictionary<string, JobKey>(StringComparer.Ordinal);
        foreach (var schedule in _scheduledJobSettings) {
            var jobKey = new JobKey(schedule.Name, schedule.Group ?? JobGroups.InternalJobsGroup);
            jobKeys[HostingConventions.GetRunNowQueueName(jobKey.ToString())] = jobKey;
        }
        if (jobKeys.Count == 0) {
            return;
        }
        var runNowJob = JobBuilder.Create<RunNowJob>()
                                  .StoreDurably()
                                  .WithIdentity(name: nameof(RunNowJob), group: JobGroups.InternalJobsGroup)
                                  .SetJobData(new JobDataMap(new Dictionary<string, object> {
                                      [JobDataKeys.RunNow] = jobKeys
                                  } as IDictionary<string, object>))
                                  .Build();
        await scheduler.AddJob(runNowJob, replace: true, cancellationToken);
        var runNowTrigger = TriggerBuilder.Create()
                                          .ForJob(runNowJob)
                                          .WithIdentity(name: $"{nameof(RunNowJob)}Trigger", group: JobGroups.InternalJobsGroup)
                                          .StartNow()
                                          .WithSimpleSchedule(x => x.WithInterval(_runNowSettings.PollingInterval).RepeatForever())
                                          .Build();
        await scheduler.ScheduleJob(runNowTrigger, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken) {
        _logger.LogInformation("Queued Hosted Service is stopping.");
        await Scheduler?.Shutdown(_workerLifetimeOptions.WaitJobsToCompleteOnShutdown, cancellationToken)!;
        // After the shutdown, so the server stays listed while its jobs complete.
        if (_serverRegistry is not null) {
            await _serverRegistry.Unregister();
        }
    }
}
