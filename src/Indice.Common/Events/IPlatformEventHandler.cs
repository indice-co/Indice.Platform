namespace Indice.Events;

/// <summary>Models the event handler.</summary>
/// <typeparam name="TEvent">The type of the event raised.</typeparam>
public interface IPlatformEventHandler<TEvent> where TEvent : IPlatformEvent
{
    /// <summary>The method used to handle the event creation.</summary>
    /// <param name="event">The type of the event raised.</param>
    /// <param name="args">Arguments to communicate back to the event publisher</param>
    /// <returns>The <see cref="Task"/> that was successfully completed.</returns>
    Task Handle(TEvent @event, PlatformEventArgs args);
}

/// <summary>Extension methods on <see cref="IPlatformEventHandler{TEvent}"/>.</summary>
public static class IPlatformEventHandlerExtensions
{
    /// <summary>The method used to handle the event creation using the default setup for <see cref="PlatformEventArgs"/>.</summary>
    /// <typeparam name="TEvent"></typeparam>
    /// <param name="eventHandler">Models the event handler.</param>
    /// <param name="event">The type of the event raised.</param>
    public static Task Handle<TEvent>(this IPlatformEventHandler<TEvent> eventHandler, TEvent @event) where TEvent : IPlatformEvent =>
        eventHandler.Handle(@event, new PlatformEventArgs { ThrowOnError = false });
}
