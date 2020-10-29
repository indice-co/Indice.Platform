using System;
using System.Threading.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting
{
    internal class DequeueJob<TWorkItem> : IJob where TWorkItem : class
    {
        private readonly IMessageQueue<TWorkItem> _workItemQueue;
        private readonly TaskHandlerActivator _taskHandlerActivator;
        private readonly ILogger<DequeueJob<TWorkItem>> _logger;

        public DequeueJob(IMessageQueue<TWorkItem> workItemQueue, TaskHandlerActivator taskHandlerActivator, ILogger<DequeueJob<TWorkItem>> logger) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Dequeue job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            //var queueName = jobDataMap[JobDataKeys.QueueName].ToString();
            var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
            TWorkItem workItem = await _workItemQueue.Dequeue();
            if (workItem != null) {
                try {
                    await _taskHandlerActivator.Invoke(jobHandlerType, workItem);
                } catch (Exception exception) {
                    _logger.LogError("An error occured while processing work item '{WorkItem}'. Exception is: {Exception}", workItem, exception);
                }
            }
        }
    }
}
