using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Kpis;

/// <summary>Detailed metrics for a specific campaign, including recipient statistics and channel-specific metrics.</summary>
public class CampaignDetailsMetrics
{
    /// <summary>Statistics about message dalivery.</summary>
    public RecipientMetrics Recipient { get; set; } = new();
    /// <summary>Metrics per channel.</summary>
    public List<ChannelMetrics> Channels { get; set; } = [];
}
