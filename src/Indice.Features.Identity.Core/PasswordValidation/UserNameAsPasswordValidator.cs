using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.PasswordValidation;

/// <inheritdoc/>
public class UserNameAsPasswordValidator : UserNameAsPasswordValidator<User>
{
    /// <inheritdoc/>
    public UserNameAsPasswordValidator(IConfiguration configuration, IdentityMessageDescriber messageDescriber) : base(configuration, messageDescriber) { }
}

/// <summary>A validator that checks if the username is identical to the password for a given number of characters.</summary>
/// <typeparam name="TUser">The type of user instance.</typeparam>
public class UserNameAsPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
{
    private readonly IdentityMessageDescriber _messageDescriber;
    /// <summary>The code used when describing the <see cref="IdentityError"/>.</summary>
    public const string ErrorDescriber = "PasswordContainsUserName";

    /// <summary>Creates a new instance of <see cref="UserNameAsPasswordValidator"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    public UserNameAsPasswordValidator(IConfiguration configuration, IdentityMessageDescriber messageDescriber) {
        _messageDescriber = messageDescriber;
        MaxAllowedUserNameSubset = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(MaxAllowedUserNameSubset)) ??
                                   configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(MaxAllowedUserNameSubset));
    }

    /// <summary>Τhe maximum number of identical characters that can be simultaneously in the username and password.</summary>
    public int? MaxAllowedUserNameSubset { get; }

    /// <inheritdoc/>
    public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string? password) {
        var result = IdentityResult.Success;
        // If the option is not set, then there is no need to perform any action.
        if (!MaxAllowedUserNameSubset.HasValue) {
            return Task.FromResult(result);
        }
        if (user == null) {
            throw new ArgumentNullException(nameof(manager));
        }
        if (password == null) {
            throw new ArgumentNullException(nameof(password));
        }
        // If username is exactly the same with the password, then this is an error independently of the MaxAllowedUsernameSubset property.
        if (user.UserName!.Equals(password!, StringComparison.InvariantCultureIgnoreCase)) {
            result = IdentityResult.Failed(new IdentityError {
                Code = ErrorDescriber,
                Description = _messageDescriber.PasswordIdenticalToUserName
            });
            return Task.FromResult(result);
        }
        if (MaxAllowedUserNameSubset > password.Length) {
            return Task.FromResult(result);
        }
        var userNameSubstrings = new List<string>();
        var characterIndex = 0;
        while (characterIndex + MaxAllowedUserNameSubset + 1 <= user.UserName.Length) {
            userNameSubstrings.Add(user.UserName.Substring(characterIndex, MaxAllowedUserNameSubset.Value + 1));
            characterIndex++;
        }
        if (userNameSubstrings.Any(userNameSubstring => password.Contains(userNameSubstring, StringComparison.OrdinalIgnoreCase))) {
            result = IdentityResult.Failed(new IdentityError {
                Code = ErrorDescriber,
                Description = _messageDescriber.PasswordIdenticalToUserName
            });
        }
        return Task.FromResult(result);
    }
}
