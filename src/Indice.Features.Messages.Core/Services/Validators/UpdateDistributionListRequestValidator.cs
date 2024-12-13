using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;

namespace Indice.Features.Messages.Core.Services.Validators;

/// <summary>Contains validation logic for <see cref="UpdateDistributionListRequestValidator"/>.</summary>
public class UpdateDistributionListRequestValidator : AbstractValidator<UpdateDistributionListRequest>
{
    /// <summary>Creates a new instance of <see cref="CreateDistributionListRequestValidator"/>.</summary>
    public UpdateDistributionListRequestValidator() {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Please provide a name for the distribution list.")
            .MaximumLength(TextSizePresets.M128)
            .WithMessage($"Maximum length for name is {TextSizePresets.M128} characters.");
    }
}
