using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// Describes the mechanism that manages the input and output of items in the queue.
    /// </summary>
    public interface IWorkItemQueue<TWorkItem> where TWorkItem : WorkItemBase
    {
        /// <summary>
        /// Inserts a new item in the queue for background processing.
        /// </summary>
        /// <param name="workItem">The work item to process.</param>
        Task Enqueue(TWorkItem workItem);
        /// <summary>
        /// Gets the next available item from the queue in order to process it.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<TWorkItem> Dequeue();
    }
}
