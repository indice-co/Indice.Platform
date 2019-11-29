using System;
using Indice.AspNetCore.Identity.Authorization;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Extensions on <see cref="IdentityBuilder"/>
    /// </summary>
    public static class IdentityBuilderExtensions
    {
        /// <summary>
        /// Setup a factory that is going to be generating the claims principal.
        /// </summary>
        /// <typeparam name="TUserClaimsPrincipalFactory">The type of factory to use in order to generate the claims principal.</typeparam>
        /// <param name="builder">The type of builder for configuring identity services.</param>
        public static IdentityBuilder AddClaimsTransform<TUserClaimsPrincipalFactory>(this IdentityBuilder builder) where TUserClaimsPrincipalFactory : class, IUserClaimsPrincipalFactory<User> {
            builder.Services.AddTransient<IUserClaimsPrincipalFactory<User>, TUserClaimsPrincipalFactory>();
            return builder;
        }

        /// <summary>
        /// Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies.
        /// </summary>
        /// <typeparam name="TUser">The type of <see cref="User"/> used by the identity system.</typeparam>
        /// <param name="builder">The type of builder for configuring identity services.</param>
        public static IdentityBuilder AddExtendedSignInManager<TUser>(this IdentityBuilder builder) where TUser : User {
            builder.Services.AddAuthentication().AddCookie(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, options => {
                options.Cookie.Name = ExtendedIdentityConstants.ExtendedValidationUserIdScheme;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            });
            builder.AddSignInManager<ExtendedSignInManager<TUser>>();
            return builder;
        }

        /// <summary>
        /// Configures the cookie used by <see cref="ExtendedIdentityConstants.ExtendedValidationUserIdScheme"/>.
        /// </summary>
        /// <param name="services">The services available in the application.</param>
        /// <param name="configure">An action to configure the <see cref="CookieAuthenticationOptions"/>.</param>
        /// <returns>The services.</returns>
        public static IServiceCollection ConfigureExtendedValidationCookie(this IServiceCollection services, Action<CookieAuthenticationOptions> configure)
            => services.Configure(ExtendedIdentityConstants.ExtendedValidationUserIdScheme, configure);

        /// <summary>
        /// Configures the <see cref="IdentityOptions"/> so they can be dynamically using a database configuration.
        /// </summary>
        /// <param name="builder">The type of builder for configuring identity services.</param>
        public static IdentityBuilder AddDynamicIdentityOptions(this IdentityBuilder builder) => AddDynamicIdentityOptions<IdentityDbContext, User, IdentityRole>(builder);

        /// <summary>
        /// Configures the <see cref="IdentityOptions"/> so they can be dynamically changed using a database configuration. Uses <see cref="IdentityDbContext"/>.
        /// </summary>
        /// <param name="builder">The type of builder for configuring identity services.</param>
        public static IdentityBuilder AddDynamicIdentityOptions<TContext, TUser, TRole>(this IdentityBuilder builder)
            where TContext : IdentityDbContext<TUser, TRole>
            where TUser : User
            where TRole : IdentityRole {
            var services = builder.Services;
            var serviceProvider = services.BuildServiceProvider();
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.0#use-di-services-to-configure-options-1
            services.AddTransient(typeof(IdentityOptionsService<,,>));
            services.AddScoped(typeof(IdentityOptionsService<,,>).MakeGenericType(typeof(TContext), builder.UserType, builder.RoleType));
            services.AddOptions<IdentityOptions>().Configure<IdentityOptionsService<TContext, TUser, TRole>>(async (identityOptions, identityOptionsService) => {
                identityOptions.Password = await identityOptionsService.GetPasswordOptions();
            });
            return builder;
        }
    }
}
