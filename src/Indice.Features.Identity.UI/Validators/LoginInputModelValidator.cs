using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="LoginInputModel"/> class.</summary>
public class LoginInputModelValidator : AbstractValidator<LoginInputModel>
{
    private readonly IStringLocalizer<LoginInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="LoginInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public LoginInputModelValidator(IStringLocalizer<LoginInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.UserName).NotEmpty().WithName(_localizer["Username"]);
        RuleFor(x => x.Password).NotEmpty().WithName(_localizer["Password"]);
    }
}
