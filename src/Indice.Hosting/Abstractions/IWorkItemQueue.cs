using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWorkItemQueue<TWorkItem> where TWorkItem : WorkItem
    {
        /// <summary>
        /// Inserts a new item in the queue for background processing.
        /// </summary>
        /// <param name="workItem"></param>
        void Enqueue(TWorkItem workItem);
        /// <summary>
        /// Gets the next available item from the queue in order to process it.
        /// </summary>
        /// <returns></returns>
        Task<TWorkItem> Dequeue();
    }
}
