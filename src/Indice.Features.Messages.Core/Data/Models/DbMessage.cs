using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Inbox message entity.</summary>
public class DbMessage
{
    /// <summary>The unique identifier of the user message.</summary>
    public Guid Id { get; set; }
    /// <summary>The id of the recipient.</summary>
    public string RecipientId { get; set; }
    /// <summary>The id of the contact.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>Determines if a message is deleted by the user.</summary>
    public bool IsDeleted { get; set; }
    /// <summary>Determines if a message is read by the user.</summary>
    public bool IsRead { get; set; }
    /// <summary>The contents of the template.</summary>
    public MessageContentDictionary Content { get; set; } = [];
    /// <summary>Defines when the inbox message was read.</summary>
    public DateTimeOffset? ReadDate { get; set; }
    /// <summary>Defines when the inbox message was deleted.</summary>
    public DateTimeOffset? DeleteDate { get; set; }
    /// <summary>Foreign key to the campaign.</summary>
    public Guid CampaignId { get; set; }
    /// <summary>Navigation property pointing to the campaign.</summary>
    public virtual DbCampaign Campaign { get; set; }
}
