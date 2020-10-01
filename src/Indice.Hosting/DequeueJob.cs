using System;
using System.Threading.Tasks;
using Indice.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Indice.Hosting
{
    /// <summary>
    /// A job that polls the work items store and tries to dequeue suitable items for processing.
    /// </summary>
    public class DequeueJob<TWorkItem> : IJob where TWorkItem : WorkItem
    {
        private readonly IWorkItemQueue<TWorkItem> _workItemQueue;
        private readonly IWorkItemHandler<TWorkItem> _workItemHandler;
        private readonly ILockManager _lockManager;
        private readonly ILogger<DequeueJob<TWorkItem>> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workItemQueue"></param>
        /// <param name="workItemHandler"></param>
        /// <param name="lockManager"></param>
        /// <param name="logger"></param>
        public DequeueJob(IWorkItemQueue<TWorkItem> workItemQueue, IWorkItemHandler<TWorkItem> workItemHandler, ILockManager lockManager, ILogger<DequeueJob<TWorkItem>> logger) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _workItemHandler = workItemHandler ?? throw new ArgumentNullException(nameof(workItemHandler));
            _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task Execute(IJobExecutionContext context) {
            _logger.LogInformation("Dequeue job run at: {Timestamp}", DateTime.UtcNow);
            var name = context.JobDetail.JobDataMap[JobDataKeys.QueueName].ToString();
            var lockResult = await _lockManager.TryAquireLock(name);
            if (!lockResult.Ok) {
                return;
            }
            using (lockResult.Lock) {
                var workItem = await _workItemQueue.Dequeue();
                try {
                    await _workItemHandler.Process(workItem);
                } catch (Exception exception) {
                    _logger.LogError("An error occured while processing work item '{WorkItem}'. Exception is: {Exception}", workItem, exception);
                }
            }
        }
    }
}
