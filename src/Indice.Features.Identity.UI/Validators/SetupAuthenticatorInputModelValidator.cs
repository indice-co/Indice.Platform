using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="SetupAuthenticatorInputModel"/> class.</summary>
public class SetupAuthenticatorInputModelValidator : AbstractValidator<SetupAuthenticatorInputModel>
{
    /// <summary>Creates a new instance of <see cref="SetupAuthenticatorInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SetupAuthenticatorInputModelValidator(IdentityMessageDescriber describer) {
        ArgumentNullException.ThrowIfNull(describer);
        RuleFor(x => x.Code)
            .NotEmpty().WithName(describer.UI_Validator_VerifyPhone_Code_FieldName)
            .Matches(@"^\d{6}$").WithName(describer.UI_Validator_VerifyPhone_Code_FieldName);
    }
}
