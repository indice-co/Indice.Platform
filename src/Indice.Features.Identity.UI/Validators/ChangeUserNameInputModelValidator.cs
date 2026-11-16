using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ChangeUserNameInputModel"/> class.</summary>
public class ChangeUserNameInputModelValidator : AbstractValidator<ChangeUserNameInputModel>
{
    /// <summary>Creates a new instance of <see cref="ChangeUserNameInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
    public ChangeUserNameInputModelValidator(IdentityMessageDescriber describer, IOptionsSnapshot<IdentityOptions> identityOptions) {
        RuleFor(x => x.UserName).NotEmpty().WithName(describer.UI_Validator_Profile_UserName_FieldName);
        RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(describer.UI_Validator_Profile_UserName_FieldName).WithMessage(describer.UI_Validator_Profile_UserName_InvalidFormat);
    }
}
