using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks
{
    [PersistJobDataAfterExecution]
    internal class ScheduledJob<TTaskHandler, TState> : IJob where TTaskHandler : class where TState : class, new()
    {
        private readonly TaskHandlerActivator _taskHandlerActivator;
        private readonly ILogger<ScheduledJob<TTaskHandler, TState>> _logger;
        private readonly IScheduledTaskStore<TState> _Store;
        private readonly IConfiguration _configuration;

        public ScheduledJob(TaskHandlerActivator taskHandlerActivator, IScheduledTaskStore<TState> scheduledTaskStore, ILogger<ScheduledJob<TTaskHandler, TState>> logger, IConfiguration configuration) {
            _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
            _Store = scheduledTaskStore ?? throw new ArgumentNullException(nameof(scheduledTaskStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task Execute(IJobExecutionContext context) {
            if (_configuration.StopWorkerHost()) {
                return;
            }
            _logger.LogInformation("Scheduled job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;

            var scheduledTask = await _Store.GetById(context.JobDetail.Key.ToString());
            if (scheduledTask == null) {
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
                    WorkerId = context.Scheduler.SchedulerName
                };
            }
            scheduledTask.ExecutionCount++;
            scheduledTask.LastExecution = context.FireTimeUtc;
            scheduledTask.NextExecution = context.NextFireTimeUtc;
            scheduledTask.Status = ScheduledTaskStatus.Running;
            scheduledTask.WorkerId = context.Scheduler.SchedulerName;
            await _Store.Save(scheduledTask);
            try {
                await _taskHandlerActivator.Invoke(jobHandlerType, scheduledTask.State, context.CancellationToken);
            } catch (Exception exception) {
                scheduledTask.Errors = exception.ToString();
                _logger.LogError("An error occured while executing task '{TaskHandlerName}'. Exception is: {Exception}", jobHandlerType.Name, exception);
            } finally {
                scheduledTask.Status = ScheduledTaskStatus.Idle;
                await _Store.Save(scheduledTask);
            }
        }
    }
}
