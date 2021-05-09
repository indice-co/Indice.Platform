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
        /// <param name="visibilityDelay">Delays the sending of payload to the queue for the specified amount of time. The maximum delay can reach up to 7 days.</param>
        /// <param name="wrap">Wrap around an envelope object.</param>
        /// <param name="base64">Defines whether the message will be encoded as Base64. Default is UTF-8.</param>
        Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityDelay = null, bool wrap = true, bool base64 = false) where TEvent : class, new();
    }
}
