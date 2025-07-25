namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the list of campaigns.</summary>
public class CampaignListFilter
{
    /// <summary>The delivery channel of a campaign.</summary>
    public MessageChannelKind[]? MessageChannelKind { get; set; }
    /// <summary>Determines if a campaign is published.</summary>
    public bool? Published { get; set; }
    /// <summary>The ID of the contact to filter campaigns by.</summary>
    public Guid? ContactId { get; set; }
}
