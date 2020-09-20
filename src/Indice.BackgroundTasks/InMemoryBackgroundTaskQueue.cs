using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Indice.BackgroundTasks.Abstractions;

namespace Indice.BackgroundTasks
{
    /// <summary>
    /// An implementation of <see cref="IBackgroundTaskQueue"/> which uses a <see cref="ConcurrentDictionary{TKey, TValue}"/> as an in-memory structure to manage background tasks.
    /// </summary>
    public class InMemoryBackgroundTaskQueue : IBackgroundTaskQueue
    {
        // https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentqueue-1
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new ConcurrentQueue<Func<CancellationToken, Task>>();
        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);

        /// <inheritdoc />
        public void Enqueue(Expression<Func<CancellationToken, Task>> workItem) {
            if (workItem == null) {
                throw new ArgumentNullException(nameof(workItem));
            }
            _workItems.Enqueue(workItem.Compile());
            _signal.Release();
        }

        /// <inheritdoc />
        public async Task<Func<CancellationToken, Task>> Dequeue(CancellationToken cancellationToken) {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);
            return workItem;
        }
    }
}
