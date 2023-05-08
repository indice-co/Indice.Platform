using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddEmailInputModel"/> class.</summary>
public class PasswordExpiredInputModelValidator : AbstractValidator<PasswordExpiredInputModel>
{
    private readonly IStringLocalizer<PasswordExpiredInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="PasswordExpiredInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PasswordExpiredInputModelValidator(IStringLocalizer<PasswordExpiredInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.NewPassword).NotEmpty().WithName(_localizer["New Password"]);
        RuleFor(x => x.NewPasswordConfirmation).NotEmpty().Equal(x => x.NewPassword).WithMessage(_localizer["Field 'New Password Confirmation' should value a value equal to 'New Password'."]);
    }
}
