using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ChangePasswordInputModel"/> class.</summary>
public class ChangePasswordInputModelValidator : AbstractValidator<ChangePasswordInputModel>
{
    private readonly IStringLocalizer<ChangePasswordInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="ChangePasswordInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ChangePasswordInputModelValidator(IStringLocalizer<ChangePasswordInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.OldPassword).NotEmpty().WithName(_localizer["Old Password"]);
        RuleFor(x => x.NewPassword).NotEmpty().WithName(_localizer["New Password"]);
    }
}
