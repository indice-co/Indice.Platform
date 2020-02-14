using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.Services;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Handles validation of token requests using <see cref="TotpConstants.GrantType.Totp"/> grant type.
    /// </summary>
    public class TotpGrantValidator : IExtensionGrantValidator
    {
        private readonly ITokenValidator _validator;
        private readonly ITotpService _totpService;

        /// <summary>
        /// Creates a new instance of <see cref="TotpGrantValidator"/>.
        /// </summary>
        /// <param name="validator">Interface for the token validator.</param>
        /// <param name="totpService">Used to generate, send and verify time based one time passwords.</param>
        public TotpGrantValidator(ITokenValidator validator, ITotpService totpService) {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _totpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
        }

        /// <summary>
        /// The grant type.
        /// </summary>
        public string GrantType => TotpConstants.GrantType.Totp;

        /// <summary>
        /// Validates the token request.
        /// </summary>
        /// <param name="context">Class describing the extension grant validation context</param>
        public async Task ValidateAsync(ExtensionGrantValidationContext context) {
            var userToken = context.Request.Raw.Get("token");
            var code = context.Request.Raw.Get("code");
            var providerName = context.Request.Raw.Get("provider");
            var provider = default(TotpProviderType?);
            var reason = context.Request.Raw.Get("reason");
            if (string.IsNullOrEmpty(userToken)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Access token 'token' parameter missing from payload.");
                return;
            }
            if (string.IsNullOrEmpty(code)) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Payload must provide a valid totp 'code'.");
                return;
            }
            var validationResult = await _validator.ValidateAccessTokenAsync(userToken);
            if (validationResult.IsError) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "Access token validation failed");
                return;
            }
            if (!string.IsNullOrEmpty(providerName)) {
                if (Enum.TryParse<TotpProviderType>(providerName, true, out var pv)) {
                    provider = pv;
                } else {
                    context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest, "Unsupported 'provider'.");
                    return;
                }
            }
            // Get user's identity.
            var sub = validationResult.Claims.FirstOrDefault(x => x.Type == "sub").Value;
            var user = new ClaimsPrincipal(new ClaimsIdentity(validationResult.Claims));
            var totpResult = await _totpService.Verify(user, code, provider, reason);
            if (!totpResult.Success) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
                return;
            }
            context.Result = new GrantValidationResult(sub, GrantType);
            return;
        }
    }
}
