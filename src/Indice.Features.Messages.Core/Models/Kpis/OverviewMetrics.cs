using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;
/// <summary>
/// Represents various counters for the dashboard, including campaign and message statistics.
/// </summary>
public class OverviewMetrics
{
    /// <summary>Statistics about campaigns in the system.</summary>
    public CampaignMetrics Campaign { get; set; } = new();
    /// <summary>Statistics about contacts in the system.</summary>
    public ContactMetrics Contact { get; set; } = new();
    /// <summary>Statistics about message dalivery.</summary>
    public RecipientMetrics Recipient { get; set; } = new();
    /// <summary>Metrics per channel.</summary>
    public List<ChannelMetrics> PerChannel { get; set; } = [];
    /// <summary>Gets or sets the date and time when the statistics was last updated (calculated).</summary>
    [Description("The date and time when the statistics was last updated.")]
    public DateTimeOffset LastUpdateDate { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Messages volume per message type.</summary>
    [Description("Message volume per message type.")]
    public List<Volume<MessageType>> PerType { get; set; } = [];
    /// <summary>Messages volume per message type.</summary>
    [Description("Todays Message volume per message type.")]
    public List<Volume<MessageType>> PerTypeToday { get; set; } = [];
}
