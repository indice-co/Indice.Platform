using System;
using System.Threading.Tasks;
using Quartz;

namespace Indice.Hosting
{
    /// <summary>
    /// A job that polls the work items store and tries to dequeue suitable items for processing.
    /// </summary>
    public class DequeueJob<TWorkItem> : IJob where TWorkItem : WorkItem
    {
        private readonly IWorkItemQueue<TWorkItem> _workItemQueue;
        private readonly WorkItemHandler<TWorkItem> _workItemHandler;

        /// <summary>
        /// 
        /// </summary>
        public DequeueJob(IWorkItemQueue<TWorkItem> workItemQueue, WorkItemHandler<TWorkItem> workItemHandler) {
            _workItemQueue = workItemQueue ?? throw new ArgumentNullException(nameof(workItemQueue));
            _workItemHandler = workItemHandler ?? throw new ArgumentNullException(nameof(workItemHandler));
        }

        /// <inheritdoc />
        public async Task Execute(IJobExecutionContext context) {
            var workItem = await _workItemQueue.Dequeue();
            await _workItemHandler.Process(workItem);
        }
    }
}
