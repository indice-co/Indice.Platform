using FluentValidation;
using Indice.Features.Identity.Core.PhoneNumberValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
public class VerifyPhoneInputModelValidator : AbstractValidator<VerifyPhoneInputModel>
{
    private readonly IStringLocalizer<VerifyPhoneInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="phoneNumberValidator">Represents a validator that validates phone numbers.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public VerifyPhoneInputModelValidator(IStringLocalizer<VerifyPhoneInputModelValidator> localizer, IPhoneNumberValidator phoneNumberValidator) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        ArgumentNullException.ThrowIfNull(nameof(phoneNumberValidator));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .WithName(_localizer["Phone Number"])
            .Must((model, phone) => phoneNumberValidator.Validate($"{model.PhoneCallingCode}{phone}"))
            .WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        RuleFor(x => x.Code).NotEmpty().When(x => !x.OtpResend).WithName(_localizer["Code"]);
    }
}
