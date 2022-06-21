using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Security;
using Indice.Services;
using Microsoft.AspNetCore.Identity;

namespace Indice.AspNetCore.Identity
{
    /// <summary>An implementation of <see cref="ITotpService"/> used for development purposes, that check for a standard TOTP in user claims.</summary>
    public class DeveloperTotpService : ITotpService
    {
        private readonly TotpService _totpService;
        private readonly UserManager<User> _userManager;

        /// <summary>Constructs a new <see cref="DeveloperTotpService"/>.</summary>
        /// <param name="totpService">Used to generate, send and verify time based one time passwords.</param>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        public DeveloperTotpService(
            TotpService totpService,
            UserManager<User> userManager
        ) {
            _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        /// <inheritdoc />
        public async Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal principal) {
            if (principal is null) {
                return default;
            }
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
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null, string data = null, string classification = null, string subject = null) {
            var userId = principal?.GetSubjectId();
            if (!string.IsNullOrWhiteSpace(userId)) {
                var user = await _userManager.GetUserAsync(principal);
                if (user == null) {
                    throw new TotpServiceException($"User with id {userId} was not found.");
                }
                var userClaims = await _userManager.GetClaimsAsync(user);
                var developerTotpClaim = userClaims.FirstOrDefault(x => x.Type == BasicClaimTypes.DeveloperTotp);
                var hasDeveloperTotp = developerTotpClaim is not null && await _userManager.IsInRoleAsync(user, BasicRoleNames.Developer);
                if (hasDeveloperTotp) {
                    return TotpResult.SuccessResult;
                }
            }
            return await _totpService.Send(principal, message, channel, purpose, securityToken, phoneNumberOrEmail, data, classification);
        }

        /// <inheritdoc />
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal?.GetSubjectId();
            if (!string.IsNullOrWhiteSpace(userId)) {
                var user = await _userManager.GetUserAsync(principal);
                if (user == null) {
                    throw new TotpServiceException($"User with id {userId} was not found.");
                }
                var userClaims = await _userManager.GetClaimsAsync(user);
                var developerTotpClaim = userClaims.FirstOrDefault(x => x.Type == BasicClaimTypes.DeveloperTotp);
                if (developerTotpClaim?.Value == code) {
                    return TotpResult.SuccessResult;
                }
            }
            return await _totpService.Verify(principal, code, provider, purpose, securityToken, phoneNumberOrEmail);
        }
    }
}
