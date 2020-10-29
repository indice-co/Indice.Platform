using System;
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
        Task<T> Dequeue();
       
        /// <summary>
        /// Adds an object to the end of the <see cref="IMessageQueue{T}"/>
        /// </summary>
        /// <param name="item">
        /// The object to add to the <see cref="IMessageQueue{T}"/>.</param>
        /// <returns></returns>
        Task Enqueue(T item);
        
        /// <summary>
        /// Returns the object at the beginning of the <see cref="IMessageQueue{T}"/> without removing it.
        /// </summary>
        /// <returns>The object at the beginning of the <see cref="IMessageQueue{T}"/></returns>
        Task<T> Peek();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="IMessageQueue{T}"/>.
        /// </summary>
        /// <returns>the number of elements</returns>
        Task<int> Count();

    }
}
