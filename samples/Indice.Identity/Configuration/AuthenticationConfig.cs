using Indice.Identity.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains a method to configure authentication for the application.
    /// </summary>
    public static class AuthenticationConfig
    {
        /// <summary>
        /// Configures the authentication for the application.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static AuthenticationBuilder AddAuthenticationConfig(this IServiceCollection services) {
            var builder = services.AddAuthentication()
                                  .AddCookie()
                                  .AddLocalApi(options => {
                                      options.ExpectedScope = IdentityServerApi.Scope;
                                  });
            services.ConfigureApplicationCookie(options => {
                options.AccessDeniedPath = new PathString("/access-denied");
                options.LoginPath = new PathString("/login");
                options.LogoutPath = new PathString("/logout");
            });
            return builder;
        }
    }
}
