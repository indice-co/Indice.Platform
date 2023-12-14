namespace Indice.Events;

/// <summary>Models the event mechanism used to raise events inside the platform.</summary>
public interface IPlatformEventService
{
    /// <summary>Raises the specified event.</summary>
    /// <param name="event">The event to raise.</param>
    Task Publish(IPlatformEvent @event);
}
