using System;
using IdentityModel;
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
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.0#use-di-services-to-configure-options-1
            services.AddTransient<IdentityOptionsService>();
            services.AddOptions<IdentityOptions>().Configure<IdentityOptionsService>(async (identityOptions, identityOptionsService) => {
                identityOptions.Password = await identityOptionsService.GetPasswordOptions();
            });
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            return services.AddIdentity<User, Role>(options => {
                var lockoutOptions = configuration.GetSection(nameof(LockoutOptions)).Get<LockoutOptions>() ?? new LockoutOptions {
                    AllowedForNewUsers = true,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5),
                    MaxFailedAccessAttempts = 5
                };
                options.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
                options.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
                options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
                options.Lockout = lockoutOptions;
                options.User.RequireUniqueEmail = false;
            })
            .AddClaimsTransform<ExtendedUserClaimsPrincipalFactory<User, Role>>()
            .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
            .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
            .AddDefaultTokenProviders();
        }
    }
}
