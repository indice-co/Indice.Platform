using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="UpdateDistributionListRequestValidator"/>.</summary>
public class UpdateDistributionListRequestValidator : AbstractValidator<UpdateDistributionListRequest>
{
    /// <summary>Creates a new instance of <see cref="CreateDistributionListRequestValidator"/>.</summary>
    /// <param name="serviceProvider">Defines a mechanism for retrieving a service object; that is, an object that provides custom support to other objects.</param>
    public UpdateDistributionListRequestValidator(IServiceProvider serviceProvider) {
        var distributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the distribution list.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.");
    }
}
