using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// An implementation of <see cref="ITotpService"/> used for development purposes, that check for a standard TOTP in user claims.
    /// </summary>
    public class DeveloperTotpService : ITotpService
    {
        /// <summary>
        /// Constructs a new <see cref="DeveloperTotpService"/>.
        /// </summary>
        /// <param name="totpService">Used to generate, send and verify time based one time passwords.</param>
        /// <param name="identityDbContext"><see cref="Microsoft.EntityFrameworkCore.DbContext"/> for the Identity Framework.</param>
        public DeveloperTotpService(
            TotpService totpService,
            ExtendedIdentityDbContext<User, Role> identityDbContext
        ) {
            TotpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            DbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
        }

        /// <summary>
        /// Used to generate, send and verify time based one time passwords.
        /// </summary>
        public TotpService TotpService { get; }
        /// <summary>
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for the Identity Framework.
        /// </summary>
        public ExtendedIdentityDbContext<User, Role> DbContext { get; set; }

        /// <inheritdoc />
        public Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal) => TotpService.GetProviders(principal);

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var hasDeveloperTotp = await DbContext.UserClaims.Where(x => x.UserId == userId && x.ClaimType == BasicClaimTypes.DeveloperTotp).AnyAsync();
                if (hasDeveloperTotp) {
                    return TotpResult.SuccessResult;
                }
            }
            return await TotpService.Send(principal, message, channel, purpose, securityToken, phoneNumberOrEmail);
        }

        /// <inheritdoc />
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var developerTotpClaim = await DbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.ClaimType == BasicClaimTypes.DeveloperTotp);
                if (developerTotpClaim?.ClaimValue == code) {
                    return TotpResult.SuccessResult;
                }
            }
            return await TotpService.Verify(principal, code, provider, purpose, securityToken, phoneNumberOrEmail);
        }
    }
}
