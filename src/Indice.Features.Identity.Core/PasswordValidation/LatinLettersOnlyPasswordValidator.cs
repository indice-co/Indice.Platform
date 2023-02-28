using Indice.Extensions;
using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.PasswordValidation;

/// <inheritdoc/>
public class UnicodeCharactersPasswordValidator : UnicodeCharactersPasswordValidator<DbUser>
{
    /// <inheritdoc/>
    public UnicodeCharactersPasswordValidator(IdentityMessageDescriber messageDescriber, IConfiguration configuration) : base(messageDescriber, configuration) { }
}

/// <summary>A validator that checks that the letters contained in the password are only Latin English characters.</summary>
/// <typeparam name="TUser">The type of user instance.</typeparam>
public class UnicodeCharactersPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : DbUser
{
    private readonly IdentityMessageDescriber _messageDescriber;
    /// <summary>The code used when describing the <see cref="IdentityError"/>.</summary>
    public const string ErrorDescriber = "PasswordContainsNonUnicodeCharacters";

    /// <summary>Creates a new instance of <see cref="UnicodeCharactersPasswordValidator"/>.</summary>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public UnicodeCharactersPasswordValidator(IdentityMessageDescriber messageDescriber, IConfiguration configuration) {
        _messageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
        AllowUnicodeCharacters = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<bool?>(nameof(AllowUnicodeCharacters)) ??
                                 configuration.GetSection(nameof(PasswordOptions)).GetValue<bool?>(nameof(AllowUnicodeCharacters));
    }

    /// <summary>Indicates whether to allow non-latin characters to be included in the password. Defaults to false.</summary>
    public bool? AllowUnicodeCharacters { get; }

    /// <inheritdoc/>
    public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
        var result = IdentityResult.Success;
        if (AllowUnicodeCharacters.HasValue && AllowUnicodeCharacters.Value) {
            return Task.FromResult(result);
        }
        var isValid = password.All(x => x.IsDigit() || x.IsSpecial() || x.IsLatinLetter());
        if (!isValid || string.IsNullOrWhiteSpace(password)) {
            result = IdentityResult.Failed(new IdentityError {
                Code = ErrorDescriber,
                Description = _messageDescriber.PasswordHasNonLatinChars
            });
        }
        return Task.FromResult(result);
    }
}
