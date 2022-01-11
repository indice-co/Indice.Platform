using System;
using FluentValidation;
using Indice.AspNetCore.Features.Campaigns.Models;
using Microsoft.Extensions.DependencyInjection;

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
        public UpsertCampaignTypeRequestValidator(IServiceProvider serviceProvider) {
            var campaignService = serviceProvider.GetRequiredService<ICampaignService>();
            RuleFor(x => x.Name).NotEmpty()
                                .WithMessage("Please provide a name for the campaign type.")
                                .MustAsync(async (name, cancellationToken) => await campaignService.GetCampaignTypeByName(name) == null)
                                .WithMessage(x => $"There is already a campaign type with name '{x.Name}'.");
        }
    }
}
