using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddEmailInputModel"/> class.</summary>
public class PasswordExpiredInputModelValidator : AbstractValidator<PasswordExpiredInputModel>
{

    /// <summary>Creates a new instance of <see cref="PasswordExpiredInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to describe validation messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PasswordExpiredInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage(describer.UI_Validator_PasswordExpired_NewPassword_Empty_Error);
        RuleFor(x => x.NewPasswordConfirmation).NotEmpty().WithMessage(describer.UI_Validator_PasswordExpired_NewPasswordConfirmation_Empty_Error);
        RuleFor(x => x.NewPasswordConfirmation).Equal(x => x.NewPassword).WithMessage(describer.UI_Validator_PasswordExpired_NewPasswordConfirmation_Mismatch_Error);
    }
}
