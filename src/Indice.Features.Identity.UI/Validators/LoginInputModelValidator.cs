using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="LoginInputModel"/> class.</summary>
public class LoginInputModelValidator : AbstractValidator<LoginInputModel>
{
    /// <summary>Creates a new instance of <see cref="LoginInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public LoginInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.UserName).NotEmpty().WithName(describer.UI_Validator_Login_UserName_FieldName);
        RuleFor(x => x.Password).NotEmpty().WithName(describer.UI_Validator_Login_Password_FieldName);
    }
}
