using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>
/// Represents metrics related to a specific message channel, 
/// including total messages sent and failures.
/// </summary>
public class ChannelMetrics
{
    /// <summary>
    /// Gets or sets the type of channel used for message delivery.
    /// </summary>
    public MessageChannelKind Kind { get; set; }
    /// <summary>
    /// Total messages sent.
    /// </summary>
    [Description("Total messages sent.")]
    public int Total { get; set; }
    /// <summary>
    /// Gets or sets the number of errors encountered.
    /// </summary>
    [Description("Number of errors encountered.")]
    public int Failures { get; set; }
}
