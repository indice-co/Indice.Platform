using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// Models the handlers that 
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
