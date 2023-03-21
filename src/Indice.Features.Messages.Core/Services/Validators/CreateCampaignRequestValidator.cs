using FluentValidation;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="CreateCampaignRequest"/>.</summary>
public class CreateCampaignRequestValidator : CampaignRequestValidator<CreateCampaignRequest>
{
    /// <summary>Creates a new instance of <see cref="CreateCampaignRequestValidator"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object.</param>
    public CreateCampaignRequestValidator(IServiceProvider serviceProvider) : base(serviceProvider) {
        RuleFor(campaign => campaign.RecipientIds)
            .Must(recipientIds => recipientIds?.Count == 0)
            .When(campaign => campaign.IsGlobal) // RecipientIds property must be empty when campaign is global.
            .WithMessage("Cannot provide a list of recipients since the campaign global.");
        RuleFor(campaign => campaign)
            .Must(campaign => campaign.RecipientIds?.Count > 0 || campaign.RecipientListId.HasValue || campaign.Recipients?.Count > 0)
            .When(campaign => !campaign.IsGlobal)
            .WithMessage("Please provide recipientIds, recipientListId or recipients property.");

    }
}
