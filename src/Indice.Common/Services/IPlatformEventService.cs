namespace Indice.Services;

/// <summary>Models the event mechanism used to raise events inside the platform.</summary>
public interface IPlatformEventService
{
    /// <summary>Raises the specified event.</summary>
    /// <typeparam name="TEvent">The type of the event.</typeparam>
    /// <param name="event">The event to raise.</param>
    Task Publish<TEvent>(TEvent @event) where TEvent : IPlatformEvent;
}

/// <summary>Platform event arguments.</summary>
public class PlatformEventArgs
{
    /// <summary>Let an exception break the control flow.</summary>
    /// <remarks>
    /// This should not be relevant to 99% of the cases since the control flow is already publishing an event for something that has already happened. 
    /// State cannot be changed and compensation logic must be handled on the side of the listener.
    /// </remarks>
    public bool ThrowOnError { get; set; }
}

/// <summary>Represents an event that is raised inside the platform.</summary>
public interface IPlatformEvent { }
