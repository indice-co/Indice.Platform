using System.Collections.Generic;
using System.Threading.Tasks;
using Indice.Hosting.Models;

namespace Indice.Hosting.Services
{
    /// <summary>
    /// Not operational implementation for <see cref="IMessageQueue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of message.</typeparam>
    public class MessageQueueNoop<T> : IMessageQueue<T> where T : class
    {
        /// <inheritdoc/>
        public Task Cleanup(int? batchSize = null) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<int> Count() => Task.FromResult(0);

        /// <inheritdoc/>
        public Task<QMessage<T>> Dequeue() => Task.FromResult<QMessage<T>>(null);

        /// <inheritdoc/>
        public Task Enqueue(QMessage<T> item, bool isPoison) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task EnqueueRange(IEnumerable<QMessage<T>> items) => Task.CompletedTask;

        /// <inheritdoc/>
        public Task<T> Peek() => Task.FromResult<T>(null);
    }
}
