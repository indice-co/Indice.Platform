using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Implementation of <see cref="IEventService"/> where 
    /// </summary>
    internal class EventService : IEventService
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a new instance of <see cref="EventService"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public EventService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task Raise<TEvent>(TEvent @event) where TEvent : IIdentityServerApiEvent {
            var handler = _serviceProvider.GetService<IIdentityServerApiEventHandler<TEvent>>();
            if (handler != null) {
                handler.Handle(@event);
            }
            return Task.CompletedTask;
        }
    }
}
