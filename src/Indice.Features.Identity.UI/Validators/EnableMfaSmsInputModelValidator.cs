using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="EnableMfaSmsInputModel"/> class.</summary>
public class EnableMfaSmsInputModelValidator : AbstractValidator<EnableMfaSmsInputModel>
{
    private readonly IConfiguration _configuration;

    /// <summary>Creates a new instance of <see cref="EnableMfaSmsInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="callingCodesProvider">The provider for the supported calling codes.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public EnableMfaSmsInputModelValidator(IdentityMessageDescriber describer, IConfiguration configuration, CallingCodesProvider callingCodesProvider) {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        RuleFor(x => x.PhoneNumber).NotEmpty().WithName(describer.UI_Validator_EnableMfaSms_PhoneNumber_FieldName)
            .UserPhoneNumber(_configuration, callingCodesProvider).WithMessage(describer.UI_Validator_EnableMfaSms_PhoneNumber_InvalidFormat);
    }
}
