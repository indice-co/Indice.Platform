using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core;

public class LocalizedIdentityErrorDescriber : ExtendedIdentityErrorDescriber
{
    private readonly IStringLocalizer<LocalizedIdentityErrorDescriber> _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<LocalizedIdentityErrorDescriber> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public override IdentityError PasswordTooShort(int length) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordTooShort),
        Description = _localizer["Passwords must be at least {0} characters.", length]
    };

    public override IdentityError PasswordRequiresUpper() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper),
        Description = _localizer["Passwords must have at least one uppercase ('A'-'Z')."]
    };

    public override IdentityError PasswordRequiresDigit() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        Description = _localizer["Passwords must have at least one digit ('0'-'9')."]
    };

    public override IdentityError PasswordRequiresLower() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresLower),
        Description = _localizer["Passwords must have at least one lowercase ('a'-'z')."]
    };

    public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
        Description = _localizer["Passwords must have at least one non alphanumeric character."]
    };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
        Description = _localizer["Passwords must use at least {0} different characters.", uniqueChars]
    };

    public override IdentityError DuplicateUserName(string userName) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.DuplicateUserName),
        Description = _localizer["Username '{0}' is already in user.", userName]
    };

    public override string PasswordRequiresDigitRequirement => _localizer["A numeric character."];
    public override string PasswordRequiresLowerRequirement => _localizer["A lower case letter."];
    public override string PasswordTooShortRequirement(int length) => _localizer["At least {0} characters long.", length];
    public override string PasswordRequiresNonAlphanumericRequirement => _localizer["A non-alphanumeric character."];
    public override string PasswordRequiresUniqueCharsRequirement(int uniqueChars) => _localizer["{0} unique chars required.", uniqueChars];
    public override string PasswordRequiresUpperRequirement => _localizer["An upper case letter."];
}
