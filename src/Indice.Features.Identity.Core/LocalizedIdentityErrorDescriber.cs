using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core;
/// <summary>A version of <see cref="ExtendedIdentityErrorDescriber"/> that uses the localizer</summary>
public class LocalizedIdentityErrorDescriber : ExtendedIdentityErrorDescriber
{
    private readonly IStringLocalizer<LocalizedIdentityErrorDescriber> _localizer;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="localizer"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public LocalizedIdentityErrorDescriber(IStringLocalizer<LocalizedIdentityErrorDescriber> localizer) {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }
    /// <inheritdoc/>
    public override IdentityError PasswordTooShort(int length) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordTooShort),
        Description = _localizer["Passwords must be at least {0} characters.", length]
    };
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresUpper() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper),
        Description = _localizer["Passwords must have at least one uppercase ('A'-'Z')."]
    };
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresDigit() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        Description = _localizer["Passwords must have at least one digit ('0'-'9')."]
    };
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresLower() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresLower),
        Description = _localizer["Passwords must have at least one lowercase ('a'-'z')."]
    };
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
        Description = _localizer["Passwords must have at least one non alphanumeric character."]
    };
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
        Description = _localizer["Passwords must use at least {0} different characters.", uniqueChars]
    };
    /// <inheritdoc/>
    public override IdentityError DuplicateUserName(string userName) => new IdentityError {
        Code = nameof(IdentityErrorDescriber.DuplicateUserName),
        Description = _localizer["Username '{0}' is already in user.", userName]
    };
    /// <inheritdoc/>
    public override string PasswordRequiresDigitRequirement => _localizer["A numeric character."];
    /// <inheritdoc/>
    public override string PasswordRequiresLowerRequirement => _localizer["A lower case letter."];
    /// <inheritdoc/>
    public override string PasswordTooShortRequirement(int length) => _localizer["At least {0} characters long.", length];
    /// <inheritdoc/>
    public override string PasswordRequiresNonAlphanumericRequirement => _localizer["A non-alphanumeric character."];
    /// <inheritdoc/>
    public override string PasswordRequiresUniqueCharsRequirement(int uniqueChars) => _localizer["{0} unique chars required.", uniqueChars];
    /// <inheritdoc/>
    public override string PasswordRequiresUpperRequirement => _localizer["An upper case letter."];
}
