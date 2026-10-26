using System.ComponentModel;
using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>
/// Message delivery outcomes over a time frame: a time series of dispatched vs failed messages, totals for the time frame and
/// the equally long period before it, and a per channel breakdown.
/// </summary>
/// <remarks>
/// A message counts as dispatched when a channel provider accepted it (a successful <c>Sent</c> event) or, for the inbox channel,
/// when it was created. It counts as failed when the provider rejected it (an unsuccessful <c>Sent</c> event).
/// </remarks>
public class DeliveryMetrics
{
    /// <summary>The time frame the metrics cover.</summary>
    public SeriesTimeFrame TimeFrame { get; set; }
    /// <summary>The (inclusive) start of the time frame.</summary>
    public DateTimeOffset RangeStart { get; set; }
    /// <summary>The (exclusive) end of the time frame.</summary>
    public DateTimeOffset RangeEnd { get; set; }
    /// <summary>The granularity of <see cref="Items"/>.</summary>
    [Description("The granularity of the series items.")]
    public DeliveryInterval Interval { get; set; }
    /// <summary>One entry per interval in the time frame, oldest first. Intervals without activity are included with zero counts.</summary>
    public List<DeliverySeriesItem> Items { get; set; } = [];
    /// <summary>Totals for the time frame.</summary>
    public DeliveryTotals Current { get; set; } = new();
    /// <summary>Totals for the equally long period right before the time frame, for trend comparison.</summary>
    public DeliveryTotals Previous { get; set; } = new();
    /// <summary>Totals per channel for the time frame.</summary>
    public List<ChannelDeliveryTotals> PerChannel { get; set; } = [];
}

/// <summary>The granularity of a delivery series.</summary>
public enum DeliveryInterval
{
    /// <summary>One item per hour.</summary>
    Hour,
    /// <summary>One item per day.</summary>
    Day,
    /// <summary>One item per month.</summary>
    Month
}

/// <summary>Dispatched and failed messages in one interval of a delivery series.</summary>
public class DeliverySeriesItem
{
    /// <summary>The start of the interval (UTC).</summary>
    public DateTimeOffset Date { get; set; }
    /// <summary>Messages dispatched in the interval.</summary>
    public int Dispatched { get; set; }
    /// <summary>Messages that failed to be dispatched in the interval.</summary>
    public int Failed { get; set; }
}

/// <summary>Dispatched and failed message totals.</summary>
public class DeliveryTotals
{
    /// <summary>Messages dispatched.</summary>
    public int Dispatched { get; set; }
    /// <summary>Messages that failed to be dispatched.</summary>
    public int Failed { get; set; }
    /// <summary>All dispatch attempts.</summary>
    public int Total => Dispatched + Failed;
    /// <summary>The share of attempts that were dispatched, between 0 and 1. Zero when there were no attempts.</summary>
    public double SuccessRate => Total == 0 ? 0 : (double)Dispatched / Total;
}

/// <summary>Dispatched and failed message totals for a channel.</summary>
public class ChannelDeliveryTotals : DeliveryTotals
{
    /// <summary>The channel.</summary>
    public MessageChannelKind Kind { get; set; }
}
