using System;
using IdentityModel;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Identity.Security;
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
            services.Configure<PasswordOptions>(configuration.GetSection(nameof(PasswordOptions)));
            services.Configure<LockoutOptions>(configuration.GetSection(nameof(LockoutOptions)));
            services.Configure<SignInOptions>(configuration.GetSection(nameof(SignInOptions)));
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            return services.AddIdentity<User, Role>(options => {
                var passwordOptions = configuration.GetSection(nameof(PasswordOptions)).Get<PasswordOptions>() ?? new PasswordOptions {
                    RequireDigit = false,
                    RequiredLength = 6,
                    RequireLowercase = false,
                    RequireNonAlphanumeric = false,
                    RequireUppercase = false
                };
                var lockoutOptions = configuration.GetSection(nameof(LockoutOptions)).Get<LockoutOptions>() ?? new LockoutOptions {
                    AllowedForNewUsers = true,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5),
                    MaxFailedAccessAttempts = 5
                };
                options.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
                options.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
                options.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
                options.Lockout = lockoutOptions;
                options.Password = passwordOptions;
                options.User.RequireUniqueEmail = true;
            })
            .AddClaimsTransform<ExtendedUserClaimsPrincipalFactory<User, Role>>()
            .AddEntityFrameworkStores<ExtendedIdentityDbContext<User, Role>>()
            .AddUserStore<ExtendedUserStore<ExtendedIdentityDbContext<User, Role>, User, Role>>()
            .AddDefaultTokenProviders();
        }
    }
}
