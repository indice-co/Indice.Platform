using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddEmailInputModel"/> class.</summary>
public class AddEmailInputModelValidator : AbstractValidator<AddEmailInputModel>
{
    private readonly IStringLocalizer<AddEmailInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="AddEmailInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddEmailInputModelValidator(IStringLocalizer<AddEmailInputModelValidator> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        RuleFor(x => x.Email).EmailAddress().NotEmpty();
    }
}
