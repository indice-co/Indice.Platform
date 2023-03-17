using Indice.Hosting.Models;
using Indice.Hosting.Services;
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

    public ScheduledJob(TaskHandlerActivator taskHandlerActivator, IScheduledTaskStore<TState> scheduledTaskStore, ILogger<ScheduledJob<TTaskHandler, TState>> logger, IConfiguration configuration, ILockManager lockManager) {
        _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
        _scheduledTaskStore = scheduledTaskStore ?? throw new ArgumentNullException(nameof(scheduledTaskStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
    }

    public async Task Execute(IJobExecutionContext context) {
        _logger.LogInformation("Scheduled job run at: {TimeStamp}", DateTime.UtcNow);
        var jobDataMap = context.JobDetail.JobDataMap;
        var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
        var singleton = jobDataMap[JobDataKeys.Singleton] as bool? ?? false;
        if (singleton) {
            await _lockManager.ExclusiveRun(context.JobDetail.Key.ToString(), token => ExecuteInternal(context, jobHandlerType, token), context.CancellationToken);
            return;
        }
        // No lock is needed so execute at will.
        await ExecuteInternal(context, jobHandlerType);
    }
    
    private async Task ExecuteInternal(IJobExecutionContext context, Type jobHandlerType, CancellationToken? cancellationToken = null) {
        var scheduledTask = await _scheduledTaskStore.GetById(context.JobDetail.Key.ToString());
        if (scheduledTask?.Enabled == false) {
            return;
        }
        if (scheduledTask is null) {
            scheduledTask = new ScheduledTask<TState> {
                Id = context.JobDetail.Key.ToString(),
                Description = context.JobDetail.Description,
                ExecutionCount = 0,
                Group = context.JobDetail.Key.Group,
                LastExecution = context.FireTimeUtc,
                NextExecution = context.NextFireTimeUtc,
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
        scheduledTask.NextExecution = context.NextFireTimeUtc;
        scheduledTask.Status = ScheduledTaskStatus.Running;
        scheduledTask.WorkerId = Environment.MachineName;
        await _scheduledTaskStore.Save(scheduledTask);
        try {
            await _taskHandlerActivator.Invoke(jobHandlerType, scheduledTask.State, cancellationToken ?? context.CancellationToken);
        } catch (Exception exception) {
            scheduledTask.Errors = exception.ToString();
            scheduledTask.LastErrorDate = DateTimeOffset.UtcNow;
            _logger.LogError("An error occurred while executing task '{TaskHandlerName}'. Exception is: {Exception}", jobHandlerType.Name, exception);
        } finally {
            scheduledTask.Status = ScheduledTaskStatus.Idle;
            await _scheduledTaskStore.Save(scheduledTask);
        }
    }
}
