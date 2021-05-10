using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>
    /// Provides methods that allow application components to communicate with each other by dispatching events.
    /// </summary>
    public interface IEventDispatcher
    {
        /// <summary>
        /// Dispatches an event of the specified type in a configured Azure queue.
        /// </summary>
        /// <typeparam name="TEvent">The concrete type of the payload to send.</typeparam>
        /// <param name="payload">The actual payload data to send.</param>
        /// <param name="actingPrincipal">A <see cref="ClaimsPrincipal"/> instance that contains information about the entity that triggered the event.</param>
        /// <param name="visibilityTimeout">Delays the sending of payload to the queue for the specified amount of time. The maximum delay can reach up to 7 days.</param>
        /// <param name="wrap">Wrap around an envelope object.</param>
        Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true) where TEvent : class, new();
    }
}
