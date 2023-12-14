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
