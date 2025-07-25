using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ForgotPasswordInputModel"/> class.</summary>
public class ForgotPasswordConfirmationInputModelValidator : AbstractValidator<ForgotPasswordConfirmationInputModel>
{
    /// <summary>Creates a new instance of <see cref="ForgotPasswordConfirmationInputModelValidator"/> class.</summary>'
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ForgotPasswordConfirmationInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName(describer.UI_Validator_ForgotPasswordConfirmation_Email_FieldName);
        RuleFor(x => x.NewPassword).NotEmpty().WithName(describer.UI_Validator_ForgotPasswordConfirmation_NewPassword_FieldName);
        RuleFor(x => x.Token).NotEmpty().WithName(describer.UI_Validator_ForgotPasswordConfirmation_Token_FieldName);
    }
}
