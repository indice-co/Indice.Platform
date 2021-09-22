using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Api.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Indice.Services;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// Implementation of <see cref="IPlatformEventService"/>.
    /// </summary>
    public class EventService : IPlatformEventService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IdentityServerApiEndpointsOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="EventService"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        public EventService(IServiceProvider serviceProvider, IdentityServerApiEndpointsOptions options) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
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

    /// <summary>
    /// Models the event handler.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event raised.</typeparam>
    public interface IIdentityServerApiEventHandler<TEvent> where TEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// The method used to handle the event creation.
        /// </summary>
        /// <param name="event">The type of the event raised.</param>
        /// <returns>The <see cref="Task"/> that was successfully completed.</returns>
        Task Handle(TEvent @event);
    }
}
