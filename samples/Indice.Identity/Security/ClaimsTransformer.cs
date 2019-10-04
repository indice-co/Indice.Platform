using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Used by the <see cref="IAuthenticationService"/> for claims transformation.
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly ILogger<ClaimsTransformer> _logger;

        /// <summary>
        /// Creates an instance of <see cref="ClaimsTransformer"/>.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        public ClaimsTransformer(ILogger<ClaimsTransformer> logger) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Perfoms a transformation to the claims of the <see cref="ClaimsPrincipal"/> when he is authenticated.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/>.</param>
        /// <returns>The modified <see cref="ClaimsPrincipal"/>.</returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) {
            if (!(principal.Identity is ClaimsIdentity identity)) {
                return Task.FromResult(principal);
            }
            // Note: AuthenticateAsync caches the result from the authentication handler, but the outcome of claims transformation is not cached. 
            // If we call AuthenticateAsync twice, then claims transformation is invoked twice (and given how implemented, could easily be duplicating claims in the principal). 
            // So when we add a claim we need to check if already exists.
            var claims = identity.Claims.ToList();
            // Manipulate user claims.
            // Try to determine the unique id of the external user (issued by the provider). 
            // The most common claim type for that are the sub and the NameIdentifier claims. 
            // Depending on the external provider, some other claim type might be used.
            var subjectClaim = (claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject) ??
                                claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)) ??
                                claims.FirstOrDefault(x => x.Type == "oid");
            if (subjectClaim == null) {
                _logger.LogInformation($"Cannot determine subject for tenant.");
            }
            var userEmailClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Email);
            if (userEmailClaim == null) {
                _logger.LogInformation($"Cannot determine email for tenant.");
            }
            // We do not want to keep these claims, so discard them.
            var nameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Name);
            if (nameClaim != null) {
                identity.TryRemoveClaim(nameClaim);
            }
            var preferredUserNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PreferredUserName);
            if (preferredUserNameClaim != null) {
                identity.TryRemoveClaim(preferredUserNameClaim);
            }
            // Finally return the modified principal.
            return Task.FromResult(principal);
        }
    }
}
