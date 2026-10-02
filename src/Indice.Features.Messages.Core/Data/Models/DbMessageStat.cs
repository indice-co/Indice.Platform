using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Send statistics entity. One row holds the successful sends of a period, for a channel and a campaign type.</summary>
public class DbMessageStat
{
    /// <summary>The length of the period.</summary>
    public MessageStatGranularity Granularity { get; set; }
    /// <summary>The first day of the period (UTC).</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>The communication channel.</summary>
    public MessageChannelKind Channel { get; set; }
    /// <summary>The type of the campaign. <see cref="Guid.Empty"/> when the campaign has no type or could not be found.</summary>
    public Guid CampaignTypeId { get; set; }
    /// <summary>The name of the campaign type, captured when the row is first created.</summary>
    public string? CampaignTypeName { get; set; }
    /// <summary>The number of successful sends.</summary>
    public int SuccessfulSends { get; set; }
    /// <summary>The date and time when the row was last written.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
