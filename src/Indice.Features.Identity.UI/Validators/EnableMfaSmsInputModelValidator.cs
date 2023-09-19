using FluentValidation;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="EnableMfaSmsInputModel"/> class.</summary>
public class EnableMfaSmsInputModelValidator : AbstractValidator<EnableMfaSmsInputModel>
{
    private readonly IStringLocalizer<EnableMfaSmsInputModelValidator> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="EnableMfaSmsInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="identityUiOptions">Represents all the ui options you can use to configure the identity ui system.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public EnableMfaSmsInputModelValidator(
        IStringLocalizer<EnableMfaSmsInputModelValidator> localizer,
        IOptionsSnapshot<IdentityUIOptions> identityUiOptions,
        IConfiguration configuration
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
                .UserPhoneNumber(_configuration)
                .WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        }
    }
}
