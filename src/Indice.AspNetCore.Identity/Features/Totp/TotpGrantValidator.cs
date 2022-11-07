using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Indice.Security;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>Handles validation of token requests using <see cref="TotpConstants.GrantType.Totp"/> grant type.</summary>
    public class TotpGrantValidator : IExtensionGrantValidator
    {
        private readonly ITokenValidator _validator;
        private readonly TotpServiceFactory _totpServiceFactory;

        /// <summary>Creates a new instance of <see cref="TotpGrantValidator"/>.</summary>
        /// <param name="validator">Interface for the token validator.</param>
        /// <param name="totpServiceFactory">Used to generate, send and verify time based one time passwords.</param>
        public TotpGrantValidator(ITokenValidator validator, TotpServiceFactory totpServiceFactory) {
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _totpServiceFactory = totpServiceFactory ?? throw new ArgumentNullException(nameof(totpServiceFactory));
        }

        /// <summary>The grant type.</summary>
        public string GrantType => TotpConstants.GrantType.Totp;

        /// <summary>Validates the token request.</summary>
        /// <param name="context">Class describing the extension grant validation context</param>
        public async Task ValidateAsync(ExtensionGrantValidationContext context) {
            var userToken = context.Request.Raw.Get("token");
            var code = context.Request.Raw.Get("code");
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
            // Get user's identity.
            var sub = validationResult.Claims.FirstOrDefault(claim => claim.Type == BasicClaimTypes.Subject).Value;
            var user = new ClaimsPrincipal(new ClaimsIdentity(validationResult.Claims));
            var totpResult = await _totpServiceFactory.Create<User>().VerifyAsync(user, code, reason);
            if (!totpResult.Success) {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
                return;
            }
            context.Result = new GrantValidationResult(sub, GrantType);
            return;
        }
    }
}
