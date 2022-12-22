namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Models the event mechanism used to raise events inside the case service.
    /// </summary>
    public interface ICaseEventService
    {
        /// <summary>
        /// Publishes the specified event.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <param name="event">The event to publish.</param>
        Task Publish<TEvent>(TEvent @event)
            where TEvent : ICaseEvent;
    }
}