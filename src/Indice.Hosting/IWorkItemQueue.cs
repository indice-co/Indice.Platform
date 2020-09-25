using System;
using System.Threading;
using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWorkItemQueue<T> where T : WorkItem<T>
    {
        /// <summary>
        /// Inserts a new item in the queue for background processing.
        /// </summary>
        /// <param name="workItem"></param>
        void Enqueue(Func<CancellationToken, Task<T>> workItem);
        /// <summary>
        /// Gets the next available item from the queue in order to process it.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Func<CancellationToken, Task<T>>> Dequeue(CancellationToken cancellationToken);
    }
}
