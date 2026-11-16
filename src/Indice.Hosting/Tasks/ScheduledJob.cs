using Indice.Hosting.Models;
using Indice.Hosting.Services;
using Indice.Hosting.Diagnostics;
using Indice.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks;

[PersistJobDataAfterExecution]
internal class ScheduledJob<TTaskHandler, TState> : IJob where TTaskHandler : class where TState : class, new()
{
    private readonly TaskHandlerActivator _taskHandlerActivator;
    private readonly ILogger<ScheduledJob<TTaskHandler, TState>> _logger;
    private readonly IScheduledTaskStore<TState> _scheduledTaskStore;
    private readonly IConfiguration _configuration;
    private readonly ILockManager _lockManager;
    private readonly WorkerMetrics _metrics;

    public ScheduledJob(TaskHandlerActivator taskHandlerActivator, IScheduledTaskStore<TState> scheduledTaskStore, ILogger<ScheduledJob<TTaskHandler, TState>> logger, IConfiguration configuration, ILockManager lockManager, WorkerMetrics metrics) {
        _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
        _scheduledTaskStore = scheduledTaskStore ?? throw new ArgumentNullException(nameof(scheduledTaskStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task Execute(IJobExecutionContext context) {
        _logger.LogInformation("Scheduled job run at: {TimeStamp}", DateTime.UtcNow);
        var jobDataMap = context.JobDetail.JobDataMap;
        var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
        var singleton = jobDataMap[JobDataKeys.Singleton] as bool? ?? false;
        if (singleton) {
            await _lockManager.ExclusiveRun(context.JobDetail.Key.ToString(), token => ExecuteInternal(context, jobHandlerType!, token), context.CancellationToken);
            return;
        }
        // No lock is needed so execute at will.
        await ExecuteInternal(context, jobHandlerType!);
    }
    
    private async Task ExecuteInternal(IJobExecutionContext context, Type jobHandlerType, CancellationToken? cancellationToken = null) {
        var scheduledTask = await _scheduledTaskStore.GetById(context.JobDetail.Key.ToString());
        // A run now request is explicit, so it also runs a disabled task: disabling only pauses the schedule.
        var runNow = context.MergedJobDataMap.TryGetValue(JobDataKeys.RunNow, out var runNowValue) && runNowValue is true;
        if (scheduledTask?.Enabled == false && !runNow) {
            return;
        }
        // A run now fires through a one-off trigger that has no next fire time, so take it from the schedule.
        var nextExecution = runNow ? await GetNextScheduledFireTime(context) : context.NextFireTimeUtc;
        if (scheduledTask is null) {
            scheduledTask = new ScheduledTask<TState> {
                Id = context.JobDetail.Key.ToString(),
                Description = context.JobDetail.Description,
                ExecutionCount = 0,
                Group = context.JobDetail.Key.Group,
                LastExecution = context.FireTimeUtc,
                NextExecution = nextExecution,
                Progress = 0,
                State = new TState(),
                Status = ScheduledTaskStatus.Running,
                Type = context.JobDetail.JobType.ToString(),
                WorkerId = Environment.MachineName,
                Enabled = true
            };
        }
        scheduledTask.ExecutionCount++;
        scheduledTask.LastExecution = context.FireTimeUtc;
        scheduledTask.NextExecution = nextExecution;
        scheduledTask.Status = ScheduledTaskStatus.Running;
        scheduledTask.WorkerId = Environment.MachineName;
        await _scheduledTaskStore.Save(scheduledTask);
        var startTimestamp = _metrics.TaskStarted(scheduledTask.Id);
        var failed = false;
        try {
            await _taskHandlerActivator.Invoke(jobHandlerType, scheduledTask.State, cancellationToken ?? context.CancellationToken);
        } catch (Exception exception) {
            failed = true;
            scheduledTask.Errors = exception.ToString();
            scheduledTask.LastErrorDate = DateTimeOffset.UtcNow;
            _logger.LogError("An error occurred while executing task '{TaskHandlerName}'. Exception is: {Exception}", jobHandlerType.Name, exception);
        } finally {
            _metrics.TaskCompleted(scheduledTask.Id, startTimestamp, failed);
            scheduledTask.Status = ScheduledTaskStatus.Idle;
            await _scheduledTaskStore.Save(scheduledTask);
        }
    }

    private static async Task<DateTimeOffset?> GetNextScheduledFireTime(IJobExecutionContext context) {
        var triggers = await context.Scheduler.GetTriggersOfJob(context.JobDetail.Key, context.CancellationToken);
        return triggers.Where(trigger => !trigger.Key.Equals(context.Trigger.Key))
                       .Select(trigger => trigger.GetNextFireTimeUtc())
                       .Where(fireTime => fireTime.HasValue)
                       .DefaultIfEmpty()
                       .Min();
    }
}
