using System;
using System.Collections.Generic;
using System.Text;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// Resolves the queue name. Uses the configured queue name through the corresponding <see cref="QueueOptions"/>. If that is empty fallback to the entityName.
    /// </summary>
    /// <typeparam name="TWorkItem">The type of the workitem in the queue</typeparam>
    public class DefaultQueueNameResolver<TWorkItem> : IQueueNameResolver<TWorkItem> where TWorkItem : class
    {
        private readonly string _Name;
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="queueOptions"></param>
        public DefaultQueueNameResolver(QueueOptions queueOptions) {
            _Name = queueOptions.QueueName;
        }

        /// <summary>
        /// Resolves the name of the queue.
        /// </summary>
        /// <returns>The name of the queue</returns>
        public string Resolve() => _Name ?? typeof(TWorkItem).Name;
    }
}
