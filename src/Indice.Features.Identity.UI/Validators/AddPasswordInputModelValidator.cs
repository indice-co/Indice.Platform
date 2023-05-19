using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddPasswordInputModel"/> class.</summary>
public class AddPasswordInputModelValidator : AbstractValidator<AddPasswordInputModel>
{
    private readonly IStringLocalizer<AddPasswordInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="AddPasswordInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPasswordInputModelValidator(IStringLocalizer<AddPasswordInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.NewPassword).NotEmpty().WithName(_localizer["New Password"]);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword).WithName(_localizer["Password Confirmation"]);
    }
}
