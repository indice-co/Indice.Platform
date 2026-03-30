using FluentValidation;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.UI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.UI.Validators;

/// <summary>Validator for <see cref="RegisterInputModel"/> class.</summary>
public class AssociateInputModelValidator : AbstractValidator<AssociateInputModel>
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
    public AssociateInputModelValidator(
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
        RuleFor(x => x.Email).EmailAddress().WithName(describer.UI_Validator_Register_Email_FieldName);
        RuleFor(x => x.HasAcceptedTerms).Equal(true).WithMessage(describer.UI_Validator_Register_AcceptTerms_Message);
        RuleFor(x => x.HasReadPrivacyPolicy).Equal(true).WithMessage(describer.UI_Validator_Register_ReadPrivacyPolicy_Message);
        RuleFor(x => x.PhoneNumber)
            .UserPhoneNumber(configuration, callingCodesProvider)
            .WithMessage(describer.UI_Validator_Register_PhoneNumber_InvalidFormat);
    }
    private bool UserNameNotBeAssignedToAnotherUser(string? userΝame) => !string.IsNullOrWhiteSpace(userΝame) && !_dbContext.Users.Any(x => x.UserName == userΝame);
}
