using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddPhoneInputModel"/> class.</summary>
public class AddPhoneInputModelValidator : AbstractValidator<AddPhoneInputModel>
{
    private readonly IStringLocalizer<AddPhoneInputModelValidator> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="AddPhoneInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="identityUiOptions">Represents all the ui options you can use to configure the identity ui system.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPhoneInputModelValidator(
        IStringLocalizer<AddPhoneInputModelValidator> localizer,
        IConfiguration configuration,
        IOptionsSnapshot<IdentityUIOptions> identityUiOptions
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

        if (identityUiOptions.Value.PhoneCountries is { } phoneCountries) {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithName(_localizer["Phone Number"])
                .UserGlobalPhoneNumber(x => x.PhoneCallingCode, identityUiOptions.Value)
                .WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        } else {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .WithName(_localizer["Phone Number"])
                .UserPhoneNumber(configuration)
                .WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        }
    }
}
