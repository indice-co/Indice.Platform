using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks
{
    [PersistJobDataAfterExecution]
    internal class DequeueJob<TWorkItem> : IJob where TWorkItem : class
    {
        private readonly IMessageQueue<TWorkItem> _workItemQueue;
        private readonly TaskHandlerActivator _taskHandlerActivator;
        private readonly ILogger<DequeueJob<TWorkItem>> _logger;
        private readonly IConfiguration _configuration;

        public DequeueJob(IMessageQueue<TWorkItem> workItemQueue, TaskHandlerActivator taskHandlerActivator, ILogger<DequeueJob<TWorkItem>> logger, IConfiguration configuration) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Dequeue job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
            var workItem = await _workItemQueue.Dequeue();
            if (workItem != null) {
                jobDataMap[JobDataKeys.BackoffIndex] = 0;
                try {
                    await _taskHandlerActivator.Invoke(jobHandlerType, jobDataMap, context.CancellationToken, workItem.Value);
                } catch (Exception exception) {
                    if (workItem.DequeueCount < 5) {
                        await _workItemQueue.ReEnqueue(workItem); // Re-enqueue to retry.
                    } else {
                        await _workItemQueue.MarkPoison(workItem); // Enqueue to poison enqueue.
                    }
                    _logger.LogError("An error occured while processing work item '{WorkItem}'. Exception is: {Exception}", workItem, exception);
                }
            } else {
                jobDataMap[JobDataKeys.BackoffIndex] = (int)(jobDataMap[JobDataKeys.BackoffIndex] ?? 0) + 1;
                Reschedule(context);
            }
        }

        /// <summary>
        /// Re-schedules the current job.
        /// </summary>
        /// <param name="context">Job execution context.</param>
        private void Reschedule(IJobExecutionContext context) {
            var jobDataMap = context.JobDetail.JobDataMap;
            var pollingInterval = jobDataMap.GetInt(JobDataKeys.PollingInterval);
            var threshold = jobDataMap.GetInt(JobDataKeys.MaxPollingInterval);
            var backoffIndex = jobDataMap.GetInt(JobDataKeys.BackoffIndex);
            var backoffTime = Enumerable.Range(1, backoffIndex + 1).Select(x => x * pollingInterval).Sum();
            if (threshold < backoffTime) {
                backoffTime = threshold;
                jobDataMap[JobDataKeys.BackoffIndex] = backoffIndex - 1;
            }
            _logger.LogInformation("Backoff: {time}", backoffTime);
            // Get the next execution date.
            var nextExecutionDate = DateTime.Now.AddMilliseconds(backoffTime);
            // Get the current trigger.
            var currentTrigger = context.Trigger;
            // Get a new builder instance from the current trigger.
            var builder = currentTrigger.GetTriggerBuilder();
            // Create a new trigger instance using the builder from the current trigger and set its start time to the next executed date obtained before.
            // This will use the same configuration parameters.
            var newTrigger = builder
                .StartAt(nextExecutionDate)
                .ForJob(context.JobDetail)
                .Build();
            // Re-schedule the job using the current trigger key and the new trigger configuration.
            context.Scheduler.RescheduleJob(currentTrigger.Key, newTrigger);
        }
    }
}
