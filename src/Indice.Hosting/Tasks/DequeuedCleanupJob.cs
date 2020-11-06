using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting.Tasks
{
    [PersistJobDataAfterExecution]
    internal class DequeuedCleanupJob<TWorkItem> : IJob where TWorkItem : class
    {
        private readonly IMessageQueue<TWorkItem> _workItemQueue;
        private readonly ILogger<DequeuedCleanupJob<TWorkItem>> _logger;

        public DequeuedCleanupJob(IMessageQueue<TWorkItem> workItemQueue, ILogger<DequeuedCleanupJob<TWorkItem>> logger) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Queue cleanup job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            var queueName = jobDataMap.GetString(JobDataKeys.QueueName);
            var cleanUpBatchSize = jobDataMap.GetInt(JobDataKeys.CleanUpBatchSize);
            try {
                await _workItemQueue.Cleanup(cleanUpBatchSize);
            } catch (Exception exception) {
                _logger.LogError("An error occured while Cleaning up queue '{Quename}'. Exception is: {Exception}", queueName, exception);
            }
        }
    }
}
