using IdentityModel;
using Indice.Features.Cases;
using Indice.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Indice.Cases.Configuation
{
    public static class AuthorizationConfig
    {
        public static IServiceCollection AddAuthorizationConfig(this IServiceCollection services) {
            return services.AddAuthorization(options => {
                options.AddPolicy(CasesApiConstants.Policies.BeCasesUser, policy => {
                    policy.AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
                        .RequireAuthenticatedUser();
                });
                options.AddPolicy(CasesApiConstants.Policies.BeCasesManager, policy => {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                          // require user to be Authenticated
                          .RequireAuthenticatedUser()
                          // also, require user to obtain a claim with the correct scope
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, CasesApiConstants.Scope));
                });
                options.AddPolicy(CasesApiConstants.Policies.BeCasesAdministrator, policy => {
                    policy.AddAuthenticationSchemes(CasesApiConstants.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.IsAdmin());
                });
            });
        }
    }

}
