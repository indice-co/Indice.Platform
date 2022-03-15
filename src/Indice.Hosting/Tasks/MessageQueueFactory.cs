using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// Provides instances of <see cref="IMessageQueue{T}"/>.
    /// </summary>
    public class MessageQueueFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance of <see cref="MessageQueueFactory"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is null.</exception>
        public MessageQueueFactory(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates a new instance of <see cref="IMessageQueue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of queue item.</typeparam>
        public IMessageQueue<T> Create<T>() where T : class => _serviceProvider.GetService<IMessageQueue<T>>();
    }
}
