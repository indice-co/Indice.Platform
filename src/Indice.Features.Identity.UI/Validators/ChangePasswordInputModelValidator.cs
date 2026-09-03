using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ChangePasswordInputModel"/> class.</summary>
public class ChangePasswordInputModelValidator : AbstractValidator<ChangePasswordInputModel>
{
    /// <summary>Creates a new instance of <see cref="ChangePasswordInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ChangePasswordInputModelValidator(IdentityMessageDescriber describer, IOptions<IdentityUIOptions> identityUiOptions) {
        RuleFor(x => x.OldPassword).NotEmpty().WithName(describer.UI_Validator_ChangePassword_OldPassword_FieldName);
        RuleFor(x => x.NewPassword).NotEmpty().WithName(describer.UI_Validator_ChangePassword_NewPassword_FieldName);
        if (identityUiOptions.Value.ShowConfirmationPassword) {
            RuleFor(x => x.NewPasswordConfirmation)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(describer.UI_Validator_ChangePassword_NewPasswordConfirmation_Empty_Error)
                .Equal(x => x.NewPassword).WithMessage(describer.UI_Validator_ChangePassword_NewPasswordConfirmation_Mismatch_Error);
        }
    }
}
