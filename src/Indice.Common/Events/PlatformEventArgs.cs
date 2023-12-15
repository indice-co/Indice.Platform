namespace Indice.Events;

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
