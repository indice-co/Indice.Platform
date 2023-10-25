using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
public class VerifyPhoneInputModelValidator : AbstractValidator<VerifyPhoneInputModel>
{
    private readonly IStringLocalizer<VerifyPhoneInputModelValidator> _localizer;
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
    /// <param name="localizer">Represents a service that provides localized strings.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="callingCodesProvider">The provider for the supported calling codes.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public VerifyPhoneInputModelValidator(IStringLocalizer<VerifyPhoneInputModelValidator> localizer, IConfiguration configuration, CallingCodesProvider callingCodesProvider) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).NotEmpty().WithName(_localizer["Phone Number"]).UserPhoneNumber(_configuration, callingCodesProvider).WithMessage(_localizer["The field '{PropertyName}' has invalid format."]);
        RuleFor(x => x.Code).NotEmpty().When(x => !x.OtpResend).WithName(_localizer["Code"]);
    }
}
