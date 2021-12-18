using FluentValidation;
using Indice.AspNetCore.Features.Campaigns.Models;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// Contains validation logic for <see cref="UpsertCampaignTypeRequest"/>.
    /// </summary>
    public class UpsertCampaignTypeRequestValidator : AbstractValidator<UpsertCampaignTypeRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="UpsertCampaignTypeRequestValidator"/>.
        /// </summary>
        public UpsertCampaignTypeRequestValidator() => RuleFor(x => x.Name).NotEmpty().WithMessage("Please provide a name for the campaign type.");
    }
}
