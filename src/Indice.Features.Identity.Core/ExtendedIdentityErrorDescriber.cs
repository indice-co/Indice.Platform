using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core;

/// <summary>Extends the default <see cref="IdentityErrorDescriber"/> adding hints for password validation rules.</summary>
public class ExtendedIdentityErrorDescriber : IdentityErrorDescriber
{

    /// <inheritdoc/>
    public override IdentityError PasswordTooShort(int length) => new () {
        Code = nameof(IdentityErrorDescriber.PasswordTooShort),
        Description = string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordTooShort, length)
    };

    /// <inheritdoc/>
    public override IdentityError PasswordRequiresUpper() => new () {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUpper),
        Description = ExtendedIdentityErrorResources.PasswordRequiresUpper
    };

    /// <inheritdoc/>
    public override IdentityError PasswordRequiresDigit() => new () {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresDigit),
        Description = ExtendedIdentityErrorResources.PasswordRequiresDigit
    };

    /// <inheritdoc/>
    public override IdentityError PasswordRequiresLower() => new () {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresLower),
        Description = ExtendedIdentityErrorResources.PasswordRequiresLower
    };
    
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresNonAlphanumeric() => new () {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric),
        Description = ExtendedIdentityErrorResources.PasswordRequiresNonAlphanumeric
    };
    
    /// <inheritdoc/>
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => new () {
        Code = nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars),
        Description = string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordRequiresUniqueChars, uniqueChars)
    };

    /// <inheritdoc/>
    public override IdentityError DuplicateUserName(string userName) => new () {
        Code = nameof(IdentityErrorDescriber.DuplicateUserName),
        Description = string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.DuplicateUserName, userName)
    };

    /// <summary>Returns an <see cref="IdentityError"/> indicating that the password has been used recently.</summary>
    /// <returns>An <see cref="IdentityError"/> with a code and description indicating that the password has been used recently.</returns>
    public virtual IdentityError PasswordRecentlyUsed() => new () {
        Code = nameof(PasswordRecentlyUsed),
        Description = ExtendedIdentityErrorResources.PasswordRecentlyUsed
    };

    /// <summary>Creates an <see cref="IdentityError"/> indicating that the provided password is too common.</summary>
    /// <returns>An <see cref="IdentityError"/> with a code and description indicating that the password is considered too
    /// common.</returns>
    public virtual IdentityError PasswordIsCommon() => new() {
        Code = nameof(PasswordIsCommon),
        Description = ExtendedIdentityErrorResources.PasswordIsCommon
    };

    /// <summary>Returns an <see cref="IdentityError"/> indicating that the password is identical to the username.</summary>
    /// <remarks>This error is typically used to enforce password policies that require passwords to differ
    /// from the username for security reasons.</remarks>
    /// <returns>An <see cref="IdentityError"/> with a code and description indicating that the password cannot be the same as
    /// the username.</returns>
    public virtual IdentityError PasswordIdenticalToUserName() => new() {
        Code = nameof(PasswordIdenticalToUserName),
        Description = ExtendedIdentityErrorResources.PasswordIdenticalToUserName
    };

    /// <summary>Returns an <see cref="IdentityError"/> indicating that the password contains non-Latin characters.</summary>
    /// <returns>An <see cref="IdentityError"/> with a code and description indicating that the password includes non-Latin
    /// characters.</returns>
    public virtual IdentityError PasswordHasNonLatinChars() => new() {
        Code = nameof(PasswordHasNonLatinChars),
        Description = ExtendedIdentityErrorResources.PasswordHasNonLatinChars
    };

    /// <summary>Creates an <see cref="IdentityError"/> indicating that the password contains characters that are not allowed.</summary>
    /// <returns>An <see cref="IdentityError"/> with a code and description specifying that the password contains disallowed
    /// characters.</returns>
    public virtual IdentityError PasswordContainsNotAllowedChars() => new() {
        Code = nameof(PasswordContainsNotAllowedChars),
        Description = ExtendedIdentityErrorResources.PasswordContainsNotAllowedChars
    };

    /// <summary>Your password's specified length does not meet the minimum length requirements.</summary>
    public virtual string PasswordTooShortRequirement(int length) => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordTooShortRequirement, length);
    /// <summary>Your password must meet the minimum number of unique chars required.</summary>
    public virtual string PasswordRequiresUniqueCharsRequirement(int uniqueChars) => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordRequiresUniqueCharsRequirement, uniqueChars);
    /// <summary>Your password must contain a non-alphanumeric character, which is required by the password policy.</summary>
    public virtual string PasswordRequiresNonAlphanumericRequirement => ExtendedIdentityErrorResources.PasswordRequiresNonAlphanumericRequirement;
    /// <summary>Your password must contain a numeric character, which is required by the password policy.</summary>
    public virtual string PasswordRequiresDigitRequirement => ExtendedIdentityErrorResources.PasswordRequiresDigitRequirement;
    /// <summary>Your password must contain a lower case letter, which is required by the password policy.</summary>
    public virtual string PasswordRequiresLowerRequirement => ExtendedIdentityErrorResources.PasswordRequiresLowerRequirement;
    /// <summary>Your password must contain an upper case letter, which is required by the password policy.</summary>
    public virtual string PasswordRequiresUpperRequirement => ExtendedIdentityErrorResources.PasswordRequiresUpperRequirement;
    /// <summary>Your password is very easy to guess, please choose a more complex one.</summary>
    public virtual string PasswordIsCommonRequirement => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordIsCommonRequirement);
    /// <summary>Your password looks a lot like your username which can lead to your account been hacked.</summary>
    public virtual string PasswordIdenticalToUserNameRequirement => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordIdenticalToUserNameRequirement);
    /// <summary>It is a good practice not to re-use your past password.</summary>
    public virtual string PasswordRecentlyUsedRequirement => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordRecentlyUsedRequirement);
    /// <summary>Your password cannot contain non-Latin characters, which is required by the password policy.</summary>
    public virtual string PasswordHasNonLatinCharsRequirement => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordHasNonLatinCharsRequirement);
    /// <summary>Not allowed characters.</summary>
    public virtual string PasswordContainsNotAllowedCharsRequirement => string.Format(IdentityResources.Culture, ExtendedIdentityErrorResources.PasswordContainsNotAllowedCharsRequirement);
}
