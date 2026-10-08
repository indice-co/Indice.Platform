using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the list of events.</summary>
public class MessageEventListFilter
{
    /// <summary>The ID of the contact to filter campaigns by.</summary>
    [Description("The ID of the campaign to filter events by.")]
    public Guid? CampaignId { get; set; }
    /// <summary>The ID of the contact to filter campaigns by.</summary>
    [Description("The ID of the message to filter events by.")]
    public Guid? MessageId { get; set; }
    /// <summary>The filter start date. If provided, only events that occurred on or after this date will be included in the results.</summary>
    [Description("The filter start date. If provided, only events that occurred on or after this date will be included in the results.")]
    public DateTimeOffset? RangeStart { get; set; }
    /// <summary>The filter end date. If provided, only events that occurred on or before this date will be included in the results.</summary>
    [Description("The filter end date. If provided, only events that occurred on or before this date will be included in the results.")]
    public DateTimeOffset? RangeEnd { get; set; }
    /// <summary>The communication channels to filter events by.</summary>
    [Description("The communication channels to filter events by.")]
    public MessageChannelKind[]? Channel { get; set; }
    /// <summary>The recipient "to" to filter events by.</summary>
    [Description("The recipient \"to\" to filter events by.")]
    public string? Recipient { get; set; }
}
