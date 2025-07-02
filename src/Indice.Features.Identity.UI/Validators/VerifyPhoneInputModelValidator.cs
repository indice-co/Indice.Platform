using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
public class VerifyPhoneInputModelValidator : AbstractValidator<VerifyPhoneInputModel>
{
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="VerifyPhoneInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="callingCodesProvider">The provider for the supported calling codes.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public VerifyPhoneInputModelValidator(IdentityMessageDescriber describer, IConfiguration configuration, CallingCodesProvider callingCodesProvider) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).NotEmpty().WithName(describer.UI_Validator_VerifyPhone_PhoneNumber_FieldName).UserPhoneNumber(_configuration, callingCodesProvider).WithMessage(describer.UI_Validator_VerifyPhone_PhoneNumber_InvalidFormat);
        RuleFor(x => x.Code).NotEmpty().When(x => !x.OtpResend).WithName( describer.UI_Validator_VerifyPhone_Code_FieldName);
    }
}
