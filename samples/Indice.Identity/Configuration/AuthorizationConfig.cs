using IdentityModel;
using IdentityServer4;
using Indice.Identity.Security;
using Indice.Security;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configures authorization policies for the application.
    /// </summary>
    public static class AuthorizationConfig
    {
        /// <summary>
        /// Configures authorization policies for the application.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddAuthorizationConfig(this IServiceCollection services) {
            return services.AddAuthorization(options => {
                options.AddPolicy(IdentityServerApi.SubScopes.Users, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerConstants.LocalApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.SubScopes.Users) || x.User.IsAdmin());
                });
            });
        }
    }
}
