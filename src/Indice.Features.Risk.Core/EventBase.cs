using Indice.Types;

namespace Indice.Features.Risk.Core;

/// <summary>Models an event that occurred in the system.</summary>
public class EventBase
{
    /// <summary>The unique id of the event.</summary>
    public Guid Id { get; set; }
    /// <summary>An amount relative to the event.</summary>
    public decimal? Amount { get; set; }
    /// <summary>The coordinates accompanying the event.</summary>
    public GeoPoint? Coordinates { get; set; }
    /// <summary>The user IP address related to the event occurred.</summary>
    public string? IpAddress { get; set; }
}
