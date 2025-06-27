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
    /// <summary>Creates a new instance of <see cref="AddPhoneInputModelValidator"/> class.</summary>
    /// <param name="describer"> The <see cref="IdentityMessageDescriber"/> used to describe validation messages.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="callingCodesProvider">The provider for the supported calling codes.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AddPhoneInputModelValidator(
        IdentityMessageDescriber describer,
        IConfiguration configuration, 
        CallingCodesProvider callingCodesProvider,
        IOptions<IdentityUIOptions> identityUiOptions
    ) {
        if (identityUiOptions.Value.EnablePhoneNumberCallingCodes) {
            RuleFor(x => x.CallingCode).NotEmpty().WithName(describer.UI_Validator_AddPhone_CallingCode_FieldName);
        }
        RuleFor(x => x.PhoneNumberWithCallingCode).NotEmpty().WithName(describer.UI_Validator_AddPhone_PhoneNumber_FieldName).UserPhoneNumber(configuration, callingCodesProvider).WithMessage(describer.UI_Validator_AddPhone_PhoneNumber_InvalidFormat);
    }
}
