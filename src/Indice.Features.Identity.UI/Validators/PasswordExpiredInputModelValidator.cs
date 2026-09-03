using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddEmailInputModel"/> class.</summary>
public class PasswordExpiredInputModelValidator : AbstractValidator<PasswordExpiredInputModel>
{

    /// <summary>Creates a new instance of <see cref="PasswordExpiredInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to describe validation messages.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public PasswordExpiredInputModelValidator(IdentityMessageDescriber describer, IOptions<IdentityUIOptions> identityUiOptions) {
        RuleFor(x => x.NewPassword).NotEmpty().WithMessage(describer.UI_Validator_PasswordExpired_NewPassword_Empty_Error);
        if (identityUiOptions.Value.ShowConfirmationPassword) {
            RuleFor(x => x.NewPasswordConfirmation)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(describer.UI_Validator_PasswordExpired_NewPasswordConfirmation_Empty_Error)
                .Equal(x => x.NewPassword).WithMessage(describer.UI_Validator_PasswordExpired_NewPasswordConfirmation_Mismatch_Error);
        }
    }
}
