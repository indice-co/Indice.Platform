using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ForgotPasswordInputModel"/> class.</summary>
public class ForgotPasswordConfirmationInputModelValidator : AbstractValidator<ForgotPasswordConfirmationInputModel>
{
    private readonly IStringLocalizer<ForgotPasswordConfirmationInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="ForgotPasswordConfirmationInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ForgotPasswordConfirmationInputModelValidator(IStringLocalizer<ForgotPasswordConfirmationInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.NewPassword).NotEmpty().WithName(_localizer["New Password"]);
        RuleFor(x => x.NewPasswordConfirmation).NotEmpty().WithName(_localizer["New Password Confirmation"]);
        RuleFor(x => x.Token).NotEmpty();
    }
}
