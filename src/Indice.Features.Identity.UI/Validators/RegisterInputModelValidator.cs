using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Indice.Globalization;
using Indice.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="RegisterInputModel"/> class.</summary>
public class RegisterInputModelValidator : AbstractValidator<RegisterInputModel>
{
    private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
    private readonly IOptionsSnapshot<IdentityOptions> _identityOptions;

    /// <summary>Creates a new instance of <see cref="LoginInputModelValidator"/> class.</summary>
    /// <param name="describer">The <see cref="IdentityMessageDescriber"/> used to provide localized error messages.</param>
    /// <param name="dbContext">An extended <see cref="DbContext"/> for the Identity framework.</param>
    /// <param name="userManager">An extendned <see cref="UserManager{TUser}"/> for the identity framework.</param>
    /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
    /// <param name="configuration">Represents the configuration element.</param>
    /// <param name="callingCodesProvider">Provides the supported Calling Codes.</param>
    /// <param name="identityUiOptions">Configuration options for Identity UI.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RegisterInputModelValidator(
        IdentityMessageDescriber describer,
        ExtendedIdentityDbContext<User, Role> dbContext,
        ExtendedUserManager<User> userManager,
        IOptionsSnapshot<IdentityOptions> identityOptions,
        IConfiguration configuration,
        CallingCodesProvider callingCodesProvider,
        IOptions<IdentityUIOptions> identityUiOptions
    ) {
        _identityOptions = identityOptions ?? throw new ArgumentNullException(nameof(identityOptions));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        RuleFor(x => x.FirstName).NotEmpty().WithName(describer.UI_Validator_Register_FirstName_FieldName);
        RuleFor(x => x.LastName).NotEmpty().WithName(describer.UI_Validator_Register_LastName_FieldName);
        if (!userManager.EmailAsUserName) {
            RuleFor(x => x.UserName).NotEmpty().WithName(describer.UI_Validator_Register_UserName_FieldName);
            RuleFor(x => x.UserName).UserName(identityOptions.Value.User).WithName(describer.UI_Validator_Register_UserName_FieldName)
                .WithMessage(describer.UI_Validator_Register_UserName_InvalidFormat);
            RuleFor(x => x.UserName).Must(UserNameNotBeAssignedToAnotherUser).WithMessage(describer.UI_Validator_Register_UserName_AlreadyExists);
        }
        RuleFor(x => x.Password).NotEmpty().WithName(describer.UI_Validator_Register_Password_FieldName);
        if (identityUiOptions.Value.ShowConfirmationPassword) {
            RuleFor(x => x.PasswordConfirmation)
                 .Cascade(CascadeMode.Stop)
                 .NotEmpty().WithMessage(describer.UI_Validator_Register_PasswordConfirmation_Empty_Error)
                 .Equal(x => x.Password).WithMessage(describer.UI_Validator_Register_PasswordConfirmation_Mismatch_Error);
        }
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithName(describer.UI_Validator_Register_Email_FieldName);
        RuleFor(x => x.Email).Must(EmailNotBeAssignedToAnotherUser).WithMessage(describer.UI_Validator_Register_Email_AlreadyExists);
        RuleFor(x => x.HasAcceptedTerms).Equal(true).WithMessage(describer.UI_Validator_Register_AcceptTerms_Message);
        RuleFor(x => x.HasReadPrivacyPolicy).Equal(true).WithMessage(describer.UI_Validator_Register_ReadPrivacyPolicy_Message);
        if (identityUiOptions.Value.EnablePhoneNumberCallingCodes) {
            RuleFor(x => x.CallingCode).NotEmpty().WithName(describer.UI_Validator_Register_CallingCode_FieldName);
        }
        RuleFor(x => x.PhoneNumberWithCallingCode)
            .UserPhoneNumber(configuration, callingCodesProvider)
            .WithMessage(describer.UI_Validator_Register_PhoneNumber_InvalidFormat);
        RuleFor(x => x.ZoneInfo).Must(ZoneInfoMustBeValid).WithMessage(describer.UI_Validator_Register_ZoneInfo_InvalidFormat);
    }

    private static bool ZoneInfoMustBeValid(string? zoneInfo) => string.IsNullOrWhiteSpace(zoneInfo) || TimeZoneInfo.TryFindSystemTimeZoneById(zoneInfo, out var result);

    private bool EmailNotBeAssignedToAnotherUser(string? email) => !_identityOptions.Value.User.RequireUniqueEmail || (!string.IsNullOrWhiteSpace(email) && !_dbContext.Users.Any(x => x.Email == email));

    private bool UserNameNotBeAssignedToAnotherUser(string? userΝame) => !string.IsNullOrWhiteSpace(userΝame) && !_dbContext.Users.Any(x => x.UserName == userΝame);
}
