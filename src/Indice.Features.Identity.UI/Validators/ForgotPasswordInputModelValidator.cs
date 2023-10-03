using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ForgotPasswordInputModel"/> class.</summary>
public class ForgotPasswordInputModelValidator : AbstractValidator<ForgotPasswordInputModel>
{
    private readonly IStringLocalizer<ForgotPasswordInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="ForgotPasswordInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ForgotPasswordInputModelValidator(IStringLocalizer<ForgotPasswordInputModelValidator> localizer, ExtendedUserManager<User> userManager) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        if (userManager.Options.User.RequireUniqueEmail) {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
        else {
            RuleFor(x => x.Email).NotEmpty();
        }
    }
}
