using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Stores;
using IdentityServer4.Validation;

namespace Indice.AspNetCore.Identity.Features
{
    internal abstract class RegistrationRequestValidatorBase<TRegistrationRequestValidationResult> where TRegistrationRequestValidationResult : ValidationResult, new()
    {
        public static readonly string[] ProtocolClaimsFilter = {
            JwtClaimTypes.AccessTokenHash,
            JwtClaimTypes.Audience,
            JwtClaimTypes.AuthorizedParty,
            JwtClaimTypes.AuthorizationCodeHash,
            JwtClaimTypes.ClientId,
            JwtClaimTypes.Expiration,
            JwtClaimTypes.IssuedAt,
            JwtClaimTypes.Issuer,
            JwtClaimTypes.JwtId,
            JwtClaimTypes.Nonce,
            JwtClaimTypes.NotBefore,
            JwtClaimTypes.ReferenceTokenId,
            JwtClaimTypes.SessionId,
            JwtClaimTypes.Scope
        };

        protected RegistrationRequestValidatorBase(
            IClientStore clientStore,
            ITokenValidator tokenValidator
        ) {
            ClientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            TokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(clientStore));
        }

        public IClientStore ClientStore { get; }
        public ITokenValidator TokenValidator { get; }

        public abstract Task<TRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters);

        protected static TRegistrationRequestValidationResult Error(string error, string errorDescription = null) => new() {
            Error = error,
            ErrorDescription = errorDescription,
            IsError = true
        };
    }
}
