using System;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity
{
    /// <inheritdoc/>
    public class LatinLettersOnlyPasswordValidator : LatinLettersOnlyPasswordValidator<User>
    {
        /// <inheritdoc/>
        public LatinLettersOnlyPasswordValidator(IdentityMessageDescriber messageDescriber, IConfiguration configuration) : base(messageDescriber, configuration) { }
    }

    /// <summary>
    /// A validator that checks that the letters contained in the password are only latin English characters.
    /// </summary>
    /// <typeparam name="TUser">The type of user instance.</typeparam>
    public class LatinLettersOnlyPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
    {
        private readonly IdentityMessageDescriber _messageDescriber;
        /// <summary>
        /// The code used when describing the <see cref="IdentityError"/>.
        /// </summary>
        public const string ErrorDescriber = "PasswordContainsNonLatinLetters";

        /// <summary>
        /// Creates a new instance of <see cref="LatinLettersOnlyPasswordValidator"/>.
        /// </summary>
        /// <param name="messageDescriber">Provides the various messages used throughout Indice packages.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public LatinLettersOnlyPasswordValidator(IdentityMessageDescriber messageDescriber, IConfiguration configuration) {
            _messageDescriber = messageDescriber ?? throw new ArgumentNullException(nameof(messageDescriber));
            AllowUnicodeCharacters = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<bool?>(nameof(AllowUnicodeCharacters)) ??
                                     configuration.GetSection(nameof(PasswordOptions)).GetValue<bool?>(nameof(AllowUnicodeCharacters));
        }

        /// <summary>
        /// Indicates whether to allow non-latin characters to be included in the password. Defaults to false.
        /// </summary>
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
}
