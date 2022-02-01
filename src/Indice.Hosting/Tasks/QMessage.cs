using System;

namespace Indice.Hosting.Tasks
{
    /// <summary>A queue message.</summary>
    /// <typeparam name="T">The message value type.</typeparam>
    public sealed class QMessage<T> where T : class
    {
        internal QMessage() { }

        /// <summary>
        /// The id
        /// </summary>
        public string Id { get; internal set; }
        /// <summary>
        /// The Queue name
        /// </summary>
        public string QueueName { get; internal set; }
        /// <summary>
        /// The payload
        /// </summary>
        public T Value { get; internal set; }
        /// <summary>
        /// The inserted date.
        /// </summary>
        public DateTime Date { get; internal set; }
        /// <summary>
        /// The dequeue count.
        /// </summary>
        public int DequeueCount { get; internal set; }
        /// <summary>
        /// A property that indicates if the message is new or already exists in the queue. Only for internal use.
        /// </summary>
        internal bool IsNew { get; set; }
    }
}
