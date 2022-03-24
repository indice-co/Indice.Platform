using System;
using FluentValidation;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Services.Validators
{
    /// <summary>
    /// Contains validation logic for <see cref="CreateDistributionListRequest"/>.
    /// </summary>
    public class CreateDistributionListRequestValidator : AbstractValidator<CreateDistributionListRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CreateDistributionListRequestValidator"/>.
        /// </summary>
        public CreateDistributionListRequestValidator(IServiceProvider serviceProvider) {
            var distributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            RuleFor(x => x.Name).NotEmpty()
                                .WithMessage("Please provide a name for the distribution list.")
                                .MaximumLength(TextSizePresets.M128)
                                .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.")
                                .MustAsync(async (name, cancellationToken) => await distributionListService.GetDistributionListByName(name) == null)
                                .WithMessage(x => $"There is already a distribution list with name '{x.Name}'.");
        }
    }
}
