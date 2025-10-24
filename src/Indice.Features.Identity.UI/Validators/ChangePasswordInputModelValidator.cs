using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ChangePasswordInputModel"/> class.</summary>
public class ChangePasswordInputModelValidator : AbstractValidator<ChangePasswordInputModel>
{
    /// <summary>Creates a new instance of <see cref="ChangePasswordInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>"
    /// <exception cref="ArgumentNullException"></exception>
    public ChangePasswordInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.OldPassword).NotEmpty().WithName(describer.UI_Validator_ChangePassword_OldPassword_FieldName);
        RuleFor(x => x.NewPassword).NotEmpty().WithName(describer.UI_Validator_ChangePassword_NewPassword_FieldName);
    }
}
