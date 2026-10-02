using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Services;

/// <summary>A number of successful sends for a UTC day, a channel and a campaign type.</summary>
public class MessageStatCount
{
    /// <summary>The UTC day of the sends.</summary>
    public DateOnly Day { get; set; }
    /// <summary>The communication channel.</summary>
    public MessageChannelKind Channel { get; set; }
    /// <summary>The type of the campaign. <see cref="Guid.Empty"/> when the campaign has no type or could not be found.</summary>
    public Guid CampaignTypeId { get; set; }
    /// <summary>The name of the campaign type.</summary>
    public string? CampaignTypeName { get; set; }
    /// <summary>The number of successful sends.</summary>
    public int Count { get; set; }
}

/// <summary>A send statistics row that the verification found different from the recount.</summary>
public class MessageStatDrift
{
    /// <summary>The length of the period.</summary>
    public MessageStatGranularity Granularity { get; set; }
    /// <summary>The first day of the period (UTC).</summary>
    public DateOnly PeriodStart { get; set; }
    /// <summary>The communication channel.</summary>
    public MessageChannelKind Channel { get; set; }
    /// <summary>The type of the campaign.</summary>
    public Guid CampaignTypeId { get; set; }
    /// <summary>The value the row had before the correction. Zero when the row did not exist.</summary>
    public int Stored { get; set; }
    /// <summary>The value after the correction. Zero when the row was deleted.</summary>
    public int Recounted { get; set; }
}
