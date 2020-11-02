using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks
{
    [PersistJobDataAfterExecution]
    internal class ScheduledJob<TTaskHandler> : IJob where TTaskHandler : class
    {
        private readonly TaskHandlerActivator _taskHandlerActivator;
        private readonly ILogger<ScheduledJob<TTaskHandler>> _logger;

        public ScheduledJob(TaskHandlerActivator taskHandlerActivator, ILogger<ScheduledJob<TTaskHandler>> logger) {
            _taskHandlerActivator = taskHandlerActivator ?? throw new ArgumentNullException(nameof(taskHandlerActivator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Scheduled job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            //var queueName = jobDataMap[JobDataKeys.QueueName].ToString();
            var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
            try {
                await _taskHandlerActivator.Invoke(jobHandlerType);
            } catch (Exception exception) {
                //await _workItemQueue.Enqueue(workItem); // enque to poison. queue.
                _logger.LogError("An error occured while executing task '{TaskHandlerName}'. Exception is: {Exception}", jobHandlerType.Name, exception);
            }
        }
    }
}
