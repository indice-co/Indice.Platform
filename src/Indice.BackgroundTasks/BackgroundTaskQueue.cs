using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Indice.BackgroundTasks.Abstractions;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly IBackgroundTaskStorage _backgroundTaskStorage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="backgroundTaskStorage"></param>
        public BackgroundTaskQueue(IBackgroundTaskStorage backgroundTaskStorage) {
            _backgroundTaskStorage = backgroundTaskStorage ?? throw new ArgumentNullException(nameof(backgroundTaskStorage));
        }

        /// <inheritdoc />
        public void Enqueue(Expression<Func<CancellationToken, Task>> workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            var backgroundTaskInfo = workItem.ToBackgroundTaskInfo();
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task<Func<CancellationToken, Task>> Dequeue(CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
    }
}
