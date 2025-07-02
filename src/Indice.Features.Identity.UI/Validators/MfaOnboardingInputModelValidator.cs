using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="MfaOnboardingInputModel"/> class.</summary>
public class MfaOnboardingInputModelValidator : AbstractValidator<MfaOnboardingInputModel>
{
    /// <summary>Creates a new instance of <see cref="MfaOnboardingInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MfaOnboardingInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.SelectedAuthenticationMethod).NotEmpty().WithName(describer.UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_FieldName)
            .WithMessage(describer.UI_Validator_MfaOnboarding_SelectedAuthenticationMethod_Required);
    }
}
