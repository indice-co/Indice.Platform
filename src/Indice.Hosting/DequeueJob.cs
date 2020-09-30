using System;
using System.Threading.Tasks;
using Indice.Services;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workItemQueue"></param>
        /// <param name="workItemHandler"></param>
        /// <param name="lockManager"></param>
        public DequeueJob(IWorkItemQueue<TWorkItem> workItemQueue, IWorkItemHandler<TWorkItem> workItemHandler, ILockManager lockManager) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _workItemHandler = workItemHandler ?? throw new ArgumentNullException(nameof(workItemHandler));
            _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        }

        /// <inheritdoc />
        public async Task Execute(IJobExecutionContext context) {
            var id = context.FireInstanceId;
            var lockResult = await _lockManager.TryAquireLockWithRetryPolicy(id, retryCount: 3, retryAfter: 5);
            using (lockResult.Lock) {
                var workItem = await _workItemQueue.Dequeue();
                await _workItemHandler.Process(workItem);
            }
        }
    }
}
