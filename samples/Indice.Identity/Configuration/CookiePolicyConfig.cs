using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods to configure application's cookie policy.
    /// </summary>
    public static class CookiePolicyConfig
    {
        /// <summary>
        /// Defines the cookie policy for the application.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static IServiceCollection AddCookiePolicyConfig(this IServiceCollection services) {
            return services.Configure<CookiePolicyOptions>(options => {
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
        }
    }
}
