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
        private readonly IdentityServerApiEndpointsOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="EventService"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        public EventService(
            IServiceProvider serviceProvider,
            IdentityServerApiEndpointsOptions options
        ) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Raise<TEvent>(TEvent @event) where TEvent : IIdentityServerApiEvent {
            if (!_options.CanRaiseEvents) {
                return Task.CompletedTask;
            }
            var handler = _serviceProvider.GetService<IIdentityServerApiEventHandler<TEvent>>();
            if (handler != null) {
                handler.Handle(@event);
            }
            return Task.CompletedTask;
        }
    }
}
