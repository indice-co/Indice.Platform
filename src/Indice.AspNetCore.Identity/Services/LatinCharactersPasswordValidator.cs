using System.Linq;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity.Services
{
    /// <inheritdoc/>
    public class LatinCharactersPasswordValidator : LatinCharactersPasswordValidator<User>
    {
        /// <summary>
        /// Creates a new instance of <see cref="LatinCharactersPasswordValidator"/>.
        /// </summary>
        public LatinCharactersPasswordValidator() : base() { }
    }

    /// <summary>
    /// A validator that checks that the letters contained in the password are only latin English characters.
    /// </summary>
    /// <typeparam name="TUser">The type of user instance.</typeparam>
    public class LatinCharactersPasswordValidator<TUser> : IPasswordValidator<TUser> where TUser : User
    {
        /// <summary>
        /// The code used when describing the <see cref="IdentityError"/>.
        /// </summary>
        public const string ErrorDescriber = "PasswordContainsNonLatinLetters";

        /// <inheritdoc/>
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password) {
            var result = IdentityResult.Success;
            var isValid = password.All(x => x.IsDigit() || x.IsSpecial() || x.IsLatinLetter());
            if (!isValid) {
                result = IdentityResult.Failed(new IdentityError {
                    Code = ErrorDescriber,
                    Description = "Password cannot contain non latin letters."
                });
            }
            return Task.FromResult(result);
        }
    }
}
