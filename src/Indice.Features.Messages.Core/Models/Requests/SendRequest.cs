using System.Text.Json.Serialization;

namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>The request model used to create a new campaign.</summary>
public class SendRequest : CreateCampaignRequest
{
    /// <summary>Determines if a campaign is published.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public override bool Published => true;

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