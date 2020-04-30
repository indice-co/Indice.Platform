using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.AspNetCore.Identity.Services
{
    /// <inheritdoc/>
    public class UserNameAsPasswordValidator : UserNameAsPasswordValidator<User>
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserNameAsPasswordValidator"/>.
        /// </summary>
        /// <param name="configuration"></param>
        public UserNameAsPasswordValidator(IConfiguration configuration) : base(configuration) { }
    }

    /// <summary>
    /// A validator that checks if the username is identical to the password for a given number of characters.
    /// </summary>
    /// <typeparam name="TUser">The type of user instance.</typeparam>
    public class UserNameAsPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
    {
        /// <summary>
        /// The code used when describing the <see cref="IdentityError"/>.
        /// </summary>
        public const string ErrorDescriber = "PasswordContainsUserName";

        /// <summary>
        /// Creates a new instance of <see cref="UserNameAsPasswordValidator{TUser}"/>.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public UserNameAsPasswordValidator(IConfiguration configuration) {
            MaxAllowedUserNameSubset = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(MaxAllowedUserNameSubset)) ??
                                       configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(MaxAllowedUserNameSubset));
        }

        /// <summary>
        /// Τhe maximum number of identical characters that can be simultaneously in the username and password.
        /// </summary>
        public int? MaxAllowedUserNameSubset { get; }

        /// <inheritdoc/>
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
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
            if (user.UserName.Equals(password, StringComparison.InvariantCultureIgnoreCase)) {
                result = IdentityResult.Failed(new IdentityError {
                    Code = ErrorDescriber,
                    Description = "Username and password cannot be the same."
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
                    Description = "Username and password are identical."
                });
            }
            return Task.FromResult(result);
        }
    }
}
