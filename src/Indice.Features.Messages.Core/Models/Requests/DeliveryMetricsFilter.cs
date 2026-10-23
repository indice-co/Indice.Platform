using System.ComponentModel;
using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Criteria for the message delivery metrics.</summary>
public class DeliveryMetricsFilter
{
    /// <summary>The time frame to aggregate. Defaults to <see cref="SeriesTimeFrame.Last30Days"/>.</summary>
    [Description("The time frame to aggregate. Defaults to the last 30 days.")]
    public SeriesTimeFrame? TimeFrame { get; set; }
    /// <summary>Limits the metrics to a single channel.</summary>
    [Description("Limits the metrics to a single channel.")]
    public MessageChannelKind? Channel { get; set; }
}
