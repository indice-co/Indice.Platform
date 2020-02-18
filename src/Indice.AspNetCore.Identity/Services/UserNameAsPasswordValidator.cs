using System;
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
        /// Creates a new instance of <see cref="UserNameAsPasswordValidator{TUser}"/>.
        /// </summary>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public UserNameAsPasswordValidator(IConfiguration configuration) {
            //AllowedUserNameCharactersOnPassword = configuration.GetSection(nameof(PasswordOptions)).GetValue<int?>(nameof(AllowedUserNameCharactersOnPassword));
            AllowedUserNameCharactersOnPassword = configuration.GetSection($"{nameof(IdentityOptions)}:{nameof(IdentityOptions.Password)}").GetValue<int?>(nameof(AllowedUserNameCharactersOnPassword));
        }

        /// <summary>
        /// Τhe maximum number of identical characters that can be simultaneously in the username and password.
        /// </summary>
        protected int? AllowedUserNameCharactersOnPassword { get; }

        /// <inheritdoc/>
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
            var result = IdentityResult.Success;
            if (!AllowedUserNameCharactersOnPassword.HasValue) {
                return Task.FromResult(result);
            }
            if (user == null) {
                throw new ArgumentNullException(nameof(manager));
            }
            if (password == null) {
                throw new ArgumentNullException(nameof(password));
            }
            var userNameChars = user.UserName.ToCharArray();
            var passwordChars = password.ToCharArray();
            var commonChars = userNameChars.Intersect(passwordChars);
            if (commonChars.Count() > AllowedUserNameCharactersOnPassword) {
                result = IdentityResult.Failed(new IdentityError {
                    Code = "UserNameInPassword",
                    Description = "Username and password are identical."
                });
            }
            return Task.FromResult(result);
        }
    }
}
