using Indice.Types;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Base class for all campaign requests.</summary>
public class CampaignRequestBase
{
    /// <summary>Determines if campaign targets all user base. Defaults to false.</summary>
    public bool IsGlobal { get; set; }
    /// <summary>The title of the campaign.</summary>
    public string Title { get; set; } = null!;
    /// <summary>The contents of the campaign.</summary>
    public MessageContentDictionary Content { get; set; } = [];
    /// <summary>Defines a (call-to-action) link.</summary>
    public Hyperlink? ActionLink { get; set; }
    /// <summary>Specifies the time period that a campaign is active.</summary>
    public Period? ActivePeriod { get; set; }
    /// <summary>The base href to access the media.</summary>
    public string? MediaBaseHref { get; set; }
    /// <summary>The id of the type or the Alias this campaign belongs.</summary>
    public GuidOrAlias? TypeId { get; set; }
    /// <summary>The id of the distribution list.</summary>
    public Guid? RecipientListId { get; set; }
    /// <summary>The Template id or the Alias  to be used for the new Campaign.</summary>
    public GuidOrAlias? MessageTemplateId { get; set; }
    /// <summary>Optional data for the campaign.</summary>
    public dynamic? Data { get; set; }
    /// <summary>If exists, it overrides the Channels collection of the specified template. </summary>
    public List<MessageChannelKind> MessageTemplateChannels { get; set; }
}
