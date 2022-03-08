using System;
using FluentValidation;
using Indice.AspNetCore.Features.Campaigns.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Services
{
    /// <summary>
    /// Contains validation logic for <see cref="UpsertMessageTypeRequest"/>.
    /// </summary>
    public class UpsertMessageTypeRequestValidator : AbstractValidator<UpsertMessageTypeRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="UpsertMessageTypeRequestValidator"/>.
        /// </summary>
        public UpsertMessageTypeRequestValidator(IServiceProvider serviceProvider) {
            var campaignService = serviceProvider.GetRequiredService<ICampaignService>();
            RuleFor(x => x.Name).NotEmpty()
                                .WithMessage("Please provide a name for the campaign type.")
                                .MustAsync(async (name, cancellationToken) => await campaignService.GetMessageTypeByName(name) == null)
                                .WithMessage(x => $"There is already a campaign type with name '{x.Name}'.");
        }
    }
}
