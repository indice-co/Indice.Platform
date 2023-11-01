using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="AddPhoneInputModel"/> class.</summary>
public class AddPhoneInputModelValidator : AbstractValidator<AddPhoneInputModel>
{
    private readonly IStringLocalizer<AddPhoneInputModelValidator> _localizer;

    /// <summary>Creates a new instance of <see cref="AddPhoneInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="callingCodesProvider">The provider for the supported calling codes.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPhoneInputModelValidator(
        IStringLocalizer<AddPhoneInputModelValidator> localizer, 
        IConfiguration configuration, 
        CallingCodesProvider callingCodesProvider,
        IOptions<IdentityUIOptions> identityUiOptions
    ) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        if (identityUiOptions.Value.EnablePhoneNumberCallingCodes) {
            RuleFor(x => x.CallingCode).NotEmpty().WithName(_localizer["Calling Code"]);
        }
        RuleFor(x => x.PhoneNumberWithCallingCode).NotEmpty().WithName(_localizer["Phone Number"]).UserPhoneNumber(configuration, callingCodesProvider).WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
    }
}
