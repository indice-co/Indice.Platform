using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using Indice.AspNetCore.Identity.Features;
using Indice.Services;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity
{
    /// <summary>
    /// 
    /// </summary>
    public class PersistedTotpService : ITotpService
    {
        /// <summary>
        /// Constructs a new <see cref="PersistedTotpService"/>.
        /// </summary>
        /// <param name="totpService">Used to generate, send and verify time based one time passwords.</param>
        /// <param name="totpDbContext">A <see cref="Microsoft.EntityFrameworkCore.DbContext"/> used to maintain the generated OTP codes of a user.</param>
        public PersistedTotpService(ITotpService totpService, TotpDbContext totpDbContext) {
            TotpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
            DbContext = totpDbContext ?? throw new ArgumentNullException(nameof(totpDbContext));
        }

        /// <summary>
        /// Used to generate, send and verify time based one time passwords.
        /// </summary>
        public ITotpService TotpService { get; }
        /// <summary>
        /// A <see cref="Microsoft.EntityFrameworkCore.DbContext"/> used to maintain the generated OTP codes of a user.
        /// </summary>
        public TotpDbContext DbContext { get; set; }

        /// <inheritdoc />
        public Task<Dictionary<string, TotpProviderMetadata>> GetProviders(ClaimsPrincipal user) => TotpService.GetProviders(user);

        /// <inheritdoc />
        public async Task<TotpResult> Send(ClaimsPrincipal principal, string message, TotpDeliveryChannel channel = TotpDeliveryChannel.Sms, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var hasPersistedCode = await DbContext.UserTotp.AnyAsync(x => x.UserId == userId);
                if (hasPersistedCode) {
                    return TotpResult.SuccessResult;
                }
            }
            return await TotpService.Send(principal, message, channel, purpose, securityToken, phoneNumberOrEmail);
        }

        /// <inheritdoc />
        public async Task<TotpResult> Verify(ClaimsPrincipal principal, string code, TotpProviderType? provider = null, string purpose = null, string securityToken = null, string phoneNumberOrEmail = null) {
            var userId = principal.GetSubjectId();
            if (!string.IsNullOrEmpty(userId)) {
                var userCode = await DbContext.UserTotp.Where(x => x.UserId == userId).Select(x => x.Code).SingleOrDefaultAsync();
                if (userCode == code) {
                    return TotpResult.SuccessResult;
                }
            }
            return await TotpService.Verify(principal, code, provider, purpose, securityToken, phoneNumberOrEmail);
        }
    }
}
