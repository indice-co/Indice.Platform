using System.Text.Json.Serialization;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new campaign.</summary>
public class SendRequest : CampaignRequestBase
{
    /// <summary>Determines if a campaign is published.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public new bool Published => true;
    /// <summary>Determines if a campaign must ignore user communication preferences.</summary>
    public bool? IgnoreUserPreferences { get; set; }
    /// <summary>Defines a list of user identifiers that constitutes the audience of the campaign.</summary>
    public List<string>? RecipientIds { get; set; }
    /// <summary>Defines a list of attachmentids already uploaded to the campaign database. These will be assiciated with the campaign.</summary>
    internal List<Guid> AttachmentIds { get; set; } = [];
    /// <summary>
    /// List of anonymous contacts not available through any of the existing contact resolvers.
    /// Use this list if recipient id is not known/available or the message will be fire and forget.
    /// </summary>
    public List<ContactAnonymous> Recipients { get; set; } = [];

    /// <summary>List of file attachments. These can only be attached to the sending channel of email and inbox.</summary>
    [JsonIgnore]
    internal List<FileAttachment> Attachments { get; set; } = [];

    internal IEnumerable<Contact> GetIncludedContacts() {
        if (RecipientIds is not null) {
            foreach (var item in RecipientIds) {
                yield return new Contact {
                    RecipientId = item
                };
            }
        }
        if (Recipients is not null) {
            foreach (var item in Recipients) {
                yield return item.ToContact();
            }
        }
    }

    /// <summary>
    /// Maps this <see cref="SendRequest"/> into a <see cref="CreateCampaignRequest"/>.
    /// Copies all relevant properties and sets <see cref="CreateCampaignRequest.Published"/> to true.
    /// </summary>
    /// <returns>A new <see cref="CreateCampaignRequest"/> instance containing the mapped data.</returns>
    public CreateCampaignRequest ToCreateCampaignRequest() {
        return new CreateCampaignRequest {
            IsGlobal = this.IsGlobal,
            Title = this.Title,
            Content = this.Content,
            ActionLink = this.ActionLink,
            ActivePeriod = this.ActivePeriod,
            MediaBaseHref = this.MediaBaseHref,
            TypeId = this.TypeId,
            RecipientListId = this.RecipientListId,
            MessageTemplateId = this.MessageTemplateId,
            Data = this.Data,
            MessageTemplateChannels = this.MessageTemplateChannels?.ToList() ?? new List<MessageChannelKind>(),
            Published = this.Published,
            IgnoreUserPreferences = this.IgnoreUserPreferences,
            RecipientIds = this.RecipientIds?.ToList(),
            Recipients = this.Recipients?.Select(r => new ContactAnonymous {
                RecipientId = r.RecipientId,
                Salutation = r.Salutation,
                FirstName = r.FirstName,
                LastName = r.LastName,
                FullName = r.FullName,
                Email = r.Email,
                PhoneNumber = r.PhoneNumber
            }).ToList() ?? new List<ContactAnonymous>(),
            AttachmentIds = this.AttachmentIds?.ToList() ?? new List<Guid>(),
        };
    }
}