using System;
using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.AspNetCore.Services.Validators
{
    /// <summary>
    /// Contains validation logic for <see cref="CreateDistributionListRequest"/>.
    /// </summary>
    public class CreateDistributionListRequestValidator : AbstractValidator<CreateDistributionListRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CreateDistributionListRequestValidator"/>.
        /// </summary>
        /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
        public CreateDistributionListRequestValidator(IServiceProvider serviceProvider) {
            var distributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Please provide a name for the distribution list.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.")
                .MustAsync(async (name, cancellationToken) => string.IsNullOrWhiteSpace(name) ? true : await distributionListService.GetByName(name) is null)
                .WithMessage(x => $"There is already a distribution list with name '{x.Name}'.");
        }
    }
}
