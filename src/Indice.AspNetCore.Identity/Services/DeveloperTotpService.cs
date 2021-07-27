using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// An implementation of <see cref="ITotpService"/> used for development purposes, that check for a standard TOTP in user claims.
    /// </summary>
    public class DeveloperTotpService : ITotpService
    {
        private readonly TotpService _totpService;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly UserManager<User> _userManager;

        /// <summary>
        /// Constructs a new <see cref="DeveloperTotpService"/>.
        /// </summary>
        /// <param name="totpService">Used to generate, send and verify time based one time passwords.</param>
        /// <param name="identityDbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        public DeveloperTotpService(
            TotpService totpService,
            ExtendedIdentityDbContext<User, Role> identityDbContext,
            UserManager<User> userManager
        ) {
            _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            _dbContext = identityDbContext ?? throw new ArgumentNullException(nameof(identityDbContext));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal) {
            var user = await _userManager.GetUserAsync(principal);
            var isDeveloper = await _userManager.IsInRoleAsync(user, BasicRoleNames.Developer);
            if (!isDeveloper) {
                return await _totpService.GetProviders(principal);
            }
            var metadata = new TotpProviderMetadata {
                Type = TotpProviderType.StandardOtp,
                Channel = TotpDeliveryChannel.None,
                DisplayName = "Standard OTP",
                CanGenerate = false
            };
            return new Dictionary<string, TotpProviderMetadata> { 
                { metadata.Name, metadata } 
            };
        }

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null, string data = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var hasDeveloperTotp = await _dbContext.UserClaims.Where(x => x.UserId == userId && x.ClaimType == BasicClaimTypes.DeveloperTotp).AnyAsync();
                if (hasDeveloperTotp) {
                    return TotpResult.SuccessResult;
                }
            }
            return await _totpService.Send(principal, message, channel, purpose, securityToken, phoneNumberOrEmail, data);
        }

        /// <inheritdoc />
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var developerTotpClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.ClaimType == BasicClaimTypes.DeveloperTotp);
                if (developerTotpClaim?.ClaimValue == code) {
                    return TotpResult.SuccessResult;
                }
            }
            return await _totpService.Verify(principal, code, provider, purpose, securityToken, phoneNumberOrEmail);
        }
    }
}
