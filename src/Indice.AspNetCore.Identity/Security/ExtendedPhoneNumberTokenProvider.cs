using System;
using System.Globalization;
using System.Security;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// TokenProvider that generates tokens from the user's security stamp and notifies a user via email. This provider is an extended version of the <see cref="PhoneNumberTokenProvider{TUser}"/> which has a
    /// configurable duration for the generated one-time password code.
    /// </summary>
    /// <typeparam name="TUser">The type used to represent a user.</typeparam>
    public class ExtendedPhoneNumberTokenProvider<TUser> : PhoneNumberTokenProvider<TUser> where TUser : User
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/>.
        /// </summary>
        public ExtendedPhoneNumberTokenProvider(Rfc6238AuthenticationService rfc6238AuthenticationService) {
            Rfc6238AuthenticationService = rfc6238AuthenticationService ?? throw new ArgumentNullException(nameof(rfc6238AuthenticationService));
        }

        /// <summary>
        /// Time-Based One-Time Password Algorithm service.
        /// </summary>
        public Rfc6238AuthenticationService Rfc6238AuthenticationService { get; set; }

        /// <inheritdoc />
        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user) {
            if (manager == null) {
                throw new ArgumentNullException(nameof(manager));
            }
            var token = await manager.CreateSecurityTokenAsync(user);
            var modifier = await GetUserModifierAsync(purpose, manager, user);
            return Rfc6238AuthenticationService.GenerateCode(token, modifier).ToString("D6", CultureInfo.InvariantCulture);
        }

        /// <inheritdoc />
        public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user) {
            if (manager == null) {
                throw new ArgumentNullException(nameof(manager));
            }
            if (!int.TryParse(token, out var code)) {
                return false;
            }
            var securityToken = await manager.CreateSecurityTokenAsync(user);
            var modifier = await GetUserModifierAsync(purpose, manager, user);
            return securityToken != null && Rfc6238AuthenticationService.ValidateCode(securityToken, code, modifier);
        }
    }
}
