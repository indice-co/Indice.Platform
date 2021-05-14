using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Identity.Security
{
    /// <summary>
    /// Used by the <see cref="IAuthenticationService"/> for claims transformation.
    /// </summary>
    public class ClaimsTransformer : IClaimsTransformation
    {
        private readonly ILogger<ClaimsTransformer> _logger;
        private readonly IdentityOptions _identityOptions;

        /// <summary>
        /// Creates an instance of <see cref="ClaimsTransformer"/>.
        /// </summary>
        /// <param name="logger">Represents a type used to perform logging.</param>
        /// <param name="identityOptions">Represents all the options you can use to configure the identity system.</param>
        public ClaimsTransformer(ILogger<ClaimsTransformer> logger, IOptions<IdentityOptions> identityOptions) {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _identityOptions = identityOptions?.Value ?? throw new ArgumentNullException(nameof(identityOptions));
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
            var idClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject) ??
                          claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier) ??
                          claims.FirstOrDefault(x => x.Type == "oid");
            if (idClaim != null) {
                identity.TryRemoveClaim(idClaim);
                identity.AddClaim(new Claim(_identityOptions.ClaimsIdentity.UserIdClaimType ?? JwtClaimTypes.Subject, idClaim.Value));
            }
            var emailClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email);
            if (emailClaim != null) {
                identity.TryRemoveClaim(emailClaim);
                identity.AddClaim(new Claim(JwtClaimTypes.Email, emailClaim.Value));
            }
            var firstName = claims.FirstOrDefault(x => x.Type == ClaimTypes.GivenName);
            if (firstName != null) {
                identity.TryRemoveClaim(firstName);
                identity.AddClaim(new Claim(JwtClaimTypes.GivenName, firstName.Value));
            }
            var surname = claims.FirstOrDefault(x => x.Type == ClaimTypes.Surname);
            if (surname != null) {
                identity.TryRemoveClaim(surname);
                identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, surname.Value));
            }
            var username = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
            if (username != null) {
                identity.TryRemoveClaim(username);
                identity.AddClaim(new Claim(_identityOptions.ClaimsIdentity.UserNameClaimType ?? JwtClaimTypes.Name, username.Value));
            }
            // We do not want to keep these claims, so discard them.
            var preferredUserNameClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.PreferredUserName);
            if (preferredUserNameClaim != null) {
                identity.TryRemoveClaim(preferredUserNameClaim);
            }
            // Finally return the modified principal.
            return Task.FromResult(principal);
        }
    }
}
