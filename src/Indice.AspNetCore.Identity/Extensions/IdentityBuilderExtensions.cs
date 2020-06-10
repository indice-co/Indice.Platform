using System;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Authorization;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Configuration;
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
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
            });
            builder.AddSignInManager<ExtendedSignInManager<TUser>>();
            return builder;
        }

        /// <summary>
        /// Registers an instance of <see cref="ExtendedSignInManager{TUser}"/> along with required dependencies, using <see cref="User"/> class as a user type..
        /// </summary>
        /// <param name="builder">The type of builder for configuring identity services.</param>
        public static IdentityBuilder AddExtendedSignInManager(this IdentityBuilder builder) => builder.AddExtendedSignInManager<User>();

        /// <summary>
        /// Adds the <see cref="ExtendedPhoneNumberTokenProvider{TUser}"/> as a default phone provider.
        /// </summary>
        /// <param name="builder">Helper functions for configuring identity services.</param>
        /// <param name="configure"></param>
        /// <returns>The configured <see cref="IdentityBuilder"/>.</returns>
        public static IdentityBuilder AddExtendedPhoneNumberTokenProvider(this IdentityBuilder builder, Action<TotpOptions> configure = null) {
            builder.Services.AddDefaultTotpService(configure);
            var providerType = typeof(ExtendedPhoneNumberTokenProvider<>).MakeGenericType(builder.UserType);
            builder.AddTokenProvider(TokenOptions.DefaultPhoneProvider, providerType);
            return builder;
        }
    }
}
