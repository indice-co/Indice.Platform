using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddPasswordInputModel"/> class.</summary>
public class AddPasswordInputModelValidator : AbstractValidator<AddPasswordInputModel>
{
    /// <summary>Creates a new instance of <see cref="AddPasswordInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to describe validation messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPasswordInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.NewPassword).NotEmpty().WithName(describer.UI_Validator_AddPassword_NewPassword_FieldName);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword).WithName(describer.UI_Validator_AddPassword_ConfirmPassword_FieldName);
    }
}
