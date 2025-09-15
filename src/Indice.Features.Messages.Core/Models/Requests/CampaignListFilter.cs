using System.ComponentModel;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the list of campaigns.</summary>
public class CampaignListFilter
{
    /// <summary>The delivery channel of a campaign.</summary>
    [Description("The delivery channel of a campaign.")]
    public MessageChannelKind[]? MessageChannelKind { get; set; }
    /// <summary>Determines if a campaign is published.</summary>
    [Description("Determines if a campaign is published.")]
    public bool? Published { get; set; }
    /// <summary>The ID of the contact to filter campaigns by.</summary>
    [Description("The ID of the contact to filter campaigns by.")]
    public Guid? ContactId { get; set; }
    /// <summary>The message type ID or alias.</summary>
    [Description("The message type ID or alias.")]
    public string[]? TypeId { get; set; }
}
