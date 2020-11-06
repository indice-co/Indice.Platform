using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Hosting
{
    /// <summary>
    /// Represents a first-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageQueue<T> where T : class
    {
        /// <summary>
        /// Removes and returns the object at the beginning of the <see cref="IMessageQueue{T}"/>.
        /// </summary>
        /// <returns>The object that is removed from the beginning of the <see cref="IMessageQueue{T}"/></returns>
        Task<QMessage<T>> Dequeue();

        /// <summary>
        /// Adds an object to the end of the <see cref="IMessageQueue{T}"/>
        /// </summary>
        /// <param name="item">
        /// The object to add to the <see cref="IMessageQueue{T}"/>. If the <paramref name="messageId"/> is specified this model will be ignored</param>
        /// <param name="messageId">Optionally add an existing item id so that a failed item can be re-processed</param>
        /// <param name="isPoison">Markes the given <paramref name="messageId"/> as poison message</param>
        /// <returns></returns>
        Task Enqueue(T item, Guid? messageId, bool isPoison);

        /// <summary>
        /// Adds a range of items to the end of the <see cref="IMessageQueue{T}"/>
        /// </summary>
        /// <param name="items">
        /// The a collection of objects to the <see cref="IMessageQueue{T}"/>.</param>
        /// <returns></returns>
        Task EnqueueRange(IEnumerable<T> items);

        /// <summary>
        /// Returns the object at the beginning of the <see cref="IMessageQueue{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="IMessageQueue{T}"/></returns>
        Task<T> Peek();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="IMessageQueue{T}"/>.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="IMessageQueue{T}"/>.</returns>
        Task<int> Count();

        /// <summary>
        /// Does cleanup of the <see cref="IMessageQueue{T}"/>.
        /// </summary>
        /// <param name="batchSize">If specified will cleanup the first <paramref name="batchSize"/> number of items</param>
        Task Cleanup(int? batchSize = null);
    }

    /// <summary>
    /// Extension methods on <see cref="IMessageQueue{T}"/>
    /// </summary>
    public static class MessageQueueExtensions
    {
        /// <summary>
        /// Shorthand dequeue to extract the payload directly
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static async Task<T> DequeueValue<T>(this IMessageQueue<T> queue) where T : class => (await queue.Dequeue())?.Value;
        
        /// <summary>
        /// Enqueue a new item
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static async Task Enqueue<T>(this IMessageQueue<T> queue, T item) where T : class => await queue.Enqueue(item, null, isPoison: false);
        
        /// <summary>
        /// Enqueue an existing item by id
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public static async Task ReEnqueue<T>(this IMessageQueue<T> queue, Guid messageId) where T : class => await queue.Enqueue(null, messageId, isPoison: false);

        /// <summary>
        /// Mark an existing message as poison. After multiple consecutive failures
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queue"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public static async Task MarkPoison<T>(this IMessageQueue<T> queue, Guid messageId) where T : class => await queue.Enqueue(null, messageId, isPoison:true);
    }
}
