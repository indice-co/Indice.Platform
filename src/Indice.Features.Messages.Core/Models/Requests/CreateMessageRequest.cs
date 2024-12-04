namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The event model used when creating a new inbox message.</summary>
public class CreateMessageRequest
{
    /// <summary>The id of the recipient.</summary>
    public string RecipientId { get; set; }
    /// <summary>The id of the contact.</summary>
    public Guid? ContactId { get; set; }
    /// <summary>The contents of the template.</summary>
    public MessageContentDictionary Content { get; set; } = [];
    /// <summary>The unique identifier of the campaign.</summary>
    public Guid CampaignId { get; set; }
}
