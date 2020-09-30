using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Hosting
{
    /// <summary>
    /// A default implementation of <see cref="IWorkItemQueue{TWorkItem}"/> which manages work items in-memory using a <see cref="ConcurrentQueue{TWorkItem}"/>.
    /// </summary>
    public class DefaultWorkItemQueue<TWorkItem> : IWorkItemQueue<TWorkItem> where TWorkItem : WorkItem
    {
        private readonly ConcurrentQueue<TWorkItem> _workItems = new ConcurrentQueue<TWorkItem>();
        private readonly ILogger<DefaultWorkItemQueue<TWorkItem>> _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public DefaultWorkItemQueue(ILogger<DefaultWorkItemQueue<TWorkItem>> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public void Enqueue(TWorkItem workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            _workItems.Enqueue(workItem);
        }

        /// <inheritdoc />
        public Task<TWorkItem> Dequeue() {
            _workItems.TryDequeue(out var workItem);
            _logger.LogInformation("Work item '{WorkItem}' was dequeued.", workItem);
            return Task.FromResult(workItem);
        }
    }
}
