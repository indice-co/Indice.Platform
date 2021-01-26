using System.IdentityModel.Tokens.Jwt;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Identity.Security;
using Indice.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods to configure application's identity system.
    /// </summary>
    public static class IdentityConfig
    {
        /// <summary>
        /// Configures various aspects of the ASP.NET Core Identity system.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
        public static IdentityBuilder AddIdentityConfig(this IServiceCollection services, IConfiguration configuration) {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            services.Configure<IdentityOptions>(configuration.GetSection(nameof(IdentityOptions)));
            return services.AddIdentity<User, Role>()
                           .AddClaimsTransform<ExtendedUserClaimsPrincipalFactory<User, Role>>()
                           .AddDefaultTokenProviders()
                           .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
                           .AddErrorDescriber<ExtendedIdentityErrorDescriber>()
                           .AddExtendedPhoneNumberTokenProvider()
                           .AddExtendedSignInManager()
                           .AddNonCommonPasswordValidator()
                           .AddPasswordValidator<LatinCharactersPasswordValidator>()
                           .AddPasswordValidator<PreviousPasswordAwareValidator<ExtendedIdentityDbContext<User, Role>, User, Role>>()
                           .AddPasswordValidator<UserNameAsPasswordValidator>()
                           .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>();
        }
    }
}
