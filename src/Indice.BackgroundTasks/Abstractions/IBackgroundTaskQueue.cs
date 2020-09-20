using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Indice.BackgroundTasks.Abstractions
{
    /// <summary>
    /// Manages the scheduling of background tasks.
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// Schedules a task which can run in the background.
        /// </summary>
        /// <param name="workItem">A unit of execution.</param>
        void Enqueue(Expression<Func<CancellationToken, Task>> workItem);
        /// <summary>
        /// Removes the background task from the scheduler.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        Task<Func<CancellationToken, Task>> Dequeue(CancellationToken cancellationToken);
    }
}
