using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddEmailInputModel"/> class.</summary>
public class AddEmailInputModelValidator : AbstractValidator<AddEmailInputModel>
{
    /// <summary>Creates a new instance of <see cref="AddEmailInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to describe validation messages.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddEmailInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.Email).EmailAddress().NotEmpty().WithName(describer.UI_Validator_AddEmail_Email_FieldName);
    }
}
