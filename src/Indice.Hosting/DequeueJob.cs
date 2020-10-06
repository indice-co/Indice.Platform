using System;
using System.Threading.Tasks;
using Indice.Hosting.Abstractions;
using Indice.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting
{
    /// <summary>
    /// A job that polls the work items store and tries to dequeue suitable items for processing.
    /// </summary>
    internal class DequeueJob<TWorkItem> : IJob where TWorkItem : WorkItem
    {
        private readonly IWorkItemQueue<TWorkItem> _workItemQueue;
        private readonly IJobHandlerFactory _jobHandlerFactory;
        private readonly ILockManager _lockManager;
        private readonly ILogger<DequeueJob<TWorkItem>> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workItemQueue"></param>
        /// <param name="jobHandlerFactory"></param>
        /// <param name="lockManager"></param>
        /// <param name="logger"></param>
        public DequeueJob(IWorkItemQueue<TWorkItem> workItemQueue, IJobHandlerFactory jobHandlerFactory, ILockManager lockManager, ILogger<DequeueJob<TWorkItem>> logger) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _jobHandlerFactory = jobHandlerFactory ?? throw new ArgumentNullException(nameof(jobHandlerFactory));
            _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Dequeue job run at: {Timestamp}", DateTime.UtcNow);
            var jobDataMap = context.JobDetail.JobDataMap;
            var queueName = jobDataMap[JobDataKeys.QueueName].ToString();
            var jobHandlerType = jobDataMap[JobDataKeys.JobHandlerType] as Type;
            var lockResult = await _lockManager.TryAquireLock(queueName);
            if (!lockResult.Ok) {
                return;
            }
            using (lockResult.Lock) {
                var workItem = await _workItemQueue.Dequeue();
                try {
                    var jobHandler = _jobHandlerFactory.Create(jobHandlerType, workItem);
                    await jobHandler.Process();
                } catch (Exception exception) {
                    _logger.LogError("An error occured while processing work item '{WorkItem}'. Exception is: {Exception}", workItem, exception);
                }
            }
        }
    }
}
