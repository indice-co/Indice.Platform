using Indice.Features.Identity.Core.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.PasswordValidation;

/// <summary>A validator that checks if the characters contained in a given password are within a list of predefined allowed characters.</summary>
public class AllowedCharactersPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
{
    /// <summary>The code used when describing the <see cref="IdentityError"/>.</summary>
    public const string ErrorDescriber = "PasswordContainsNotAllowedCharacters";

    /// <summary>Creates a new instance of <see cref="AllowedCharactersPasswordValidator{TUser}"/>.</summary>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
    public AllowedCharactersPasswordValidator(IConfiguration configuration, IdentityMessageDescriber messageDescriber) {
        AllowedCharacters = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<string>(nameof(AllowedCharacters)) ??
                            configuration.GetSection(nameof(PasswordOptions)).GetValue<string>(nameof(AllowedCharacters));
        MessageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
    }

    /// <summary>The allowed characters of a password.</summary>
    public string AllowedCharacters { get; }
    /// <summary>Provides the various messages used throughout Indice packages.</summary>
    public IdentityMessageDescriber MessageDescriber { get; }

    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
        var result = IdentityResult.Success;
        // if AllowedCharacters is not defined in appsettings.json then the validator always returns success
        if (string.IsNullOrEmpty(AllowedCharacters)) {
            return Task.FromResult(result);
        }
        var isValid = password.All(x => AllowedCharacters.Contains(x));
        if (!isValid || string.IsNullOrWhiteSpace(password)) {
            result = IdentityResult.Failed(new IdentityError {
                Code = ErrorDescriber,
                Description = MessageDescriber.PasswordContainsNotAllowedChars
            });
        }
        return Task.FromResult(result);
    }
}

/// <inheritdoc/>
public class AllowedCharactersPasswordValidator : AllowedCharactersPasswordValidator<User>
{
    /// <inheritdoc/>
    public AllowedCharactersPasswordValidator(IConfiguration configuration, IdentityMessageDescriber messageDescriber) : base(configuration, messageDescriber) { }
}
