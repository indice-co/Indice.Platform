using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="MfaOnboardingInputModel"/> class.</summary>
public class MfaOnboardingInputModelValidator : AbstractValidator<MfaOnboardingInputModel>
{
    private readonly IStringLocalizer<MfaOnboardingInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="MfaOnboardingInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MfaOnboardingInputModelValidator(IStringLocalizer<MfaOnboardingInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.SelectedAuthenticationMethod).NotEmpty().WithMessage(_localizer["Please select one of the available authentication methods."]);
    }
}
