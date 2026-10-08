using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>
/// Represents a set of criteria used to filter message event series by event type and channel kind.
/// </summary>
/// <remarks>Use this class to specify filtering options when querying or processing collections of message
/// events. Setting the properties allows you to narrow results to specific event types or message channels. All filter
/// criteria are optional; leaving a property unset will not restrict results by that criterion.</remarks>
public class MessageEventSeriesFilter
{
    /// <summary>Gets or sets a filter for type of the event.</summary>
    public string? EventType { get; set; }
    /// <summary>Gets or sets a filter for the type of message channel.</summary>
    public MessageChannelKind? Channel { get; set; }
    /// <summary>Gets or sets the time frame for aggregating message event series data.</summary>
    public SeriesTimeFrame? TimeFrame { get; set; }
}
