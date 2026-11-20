using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Indice.Validation;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ProfileDetailsInputModel"/> class.</summary>
public class ProfileDetailsInputModelValidator : AbstractValidator<ProfileDetailsInputModel>
{
    /// <summary>Creates a new instance of <see cref="ProfileDetailsInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    public ProfileDetailsInputModelValidator(IdentityMessageDescriber describer) {
        RuleFor(x => x.Tin).TaxCode("GR").WithName(describer.UI_Validator_Profile_Tin_FieldName).WithMessage(describer.UI_Validator_Profile_Tin_InvalidFormat);
    }
}
