using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.Hosting
{
    /// <summary>
    /// A default implementation of <see cref="IWorkItemQueue{TWorkItem}"/> which manages work items in-memory using a <see cref="ConcurrentQueue{TWorkItem}"/>.
    /// </summary>
    public class WorkItemQueueInMemory<TWorkItem> : IWorkItemQueue<TWorkItem> where TWorkItem : WorkItem
    {
        private readonly ConcurrentQueue<TWorkItem> _workItems = new ConcurrentQueue<TWorkItem>();
        private readonly ILogger<WorkItemQueueInMemory<TWorkItem>> _logger;

        /// <summary>
        /// Creates a new instance of <see cref="WorkItemQueueInMemory{TWorkItem}"/>.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public WorkItemQueueInMemory(ILogger<WorkItemQueueInMemory<TWorkItem>> logger) {
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
            if (workItem == null) {
                _logger.LogInformation("There are no other items available in the queue to process.");
            } else {
                _logger.LogInformation("Work item '{WorkItem}' was dequeued.", workItem);
            }
            return Task.FromResult(workItem);
        }
    }
}
