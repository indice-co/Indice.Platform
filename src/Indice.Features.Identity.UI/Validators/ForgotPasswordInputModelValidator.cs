using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ForgotPasswordInputModel"/> class.</summary>
public class ForgotPasswordInputModelValidator : AbstractValidator<ForgotPasswordInputModel>
{
    /// <summary>Creates a new instance of <see cref="ForgotPasswordInputModelValidator"/> class.</summary>
    /// <param name="describer">Provides the APIs for managing localized error messages.</param>
    /// <param name="userManager">Provides the APIs for managing users and their related data in a persistence store.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ForgotPasswordInputModelValidator(IdentityMessageDescriber describer, ExtendedUserManager<User> userManager) {
        if (userManager.Options.User.RequireUniqueEmail) {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName(describer.UI_Validator_ForgotPassword_Email_FieldName);
        }
        else {
            RuleFor(x => x.Email).NotEmpty().WithName(describer.UI_Validator_ForgotPassword_Email_FieldName);
        }
    }
}
