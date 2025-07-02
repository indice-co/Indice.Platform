using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.UI.Models;
using Indice.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="ProfileInputModelValidator"/> class.</summary>
public class ProfileInputModelValidator : AbstractValidator<ProfileInputModel>
{
    /// <summary>Creates a new instance of <see cref="ProfileInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="callingCodesProvider">Provides the supported Calling Codes.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public ProfileInputModelValidator(
        IdentityMessageDescriber describer,
        IOptionsSnapshot<IdentityOptions> identityOptions,
        IConfiguration configuration,
        CallingCodesProvider callingCodesProvider,
        IOptions<IdentityUIOptions> identityUiOptions
    ) {
        RuleFor(x => x.UserName).NotEmpty().WithName(describer.UI_Validator_Profile_UserName_FieldName);
        RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(describer.UI_Validator_Profile_UserName_FieldName).WithMessage(describer.UI_Validator_Profile_UserName_InvalidFormat);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName(describer.UI_Validator_Profile_Email_FieldName);
        if (identityUiOptions.Value.EnablePhoneNumberCallingCodes) {
            RuleFor(x => x.CallingCode).NotEmpty().WithName(describer.UI_Validator_Profile_CallingCode_FieldName);
        }
        RuleFor(x => x.PhoneNumberWithCallingCode).UserPhoneNumber(configuration, callingCodesProvider).WithName(describer.UI_Validator_Profile_PhoneNumber_FieldName).WithMessage(describer.UI_Validator_Profile_PhoneNumber_InvalidFormat);
        RuleFor(x => x.Tin).TaxCode("GR").WithName(describer.UI_Validator_Profile_Tin_FieldName).WithMessage(describer.UI_Validator_Profile_Tin_InvalidFormat);
    }
}
