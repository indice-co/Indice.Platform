using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.Identity.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity
{
    /// <summary>
    /// Extensions on <see cref="IdentityBuilder"/>
    /// </summary>
    public static class IdentityBuilderExtensions
    {

        /// <summary>
        /// Setup an factory that is going to be generating the claims principal.
        /// </summary>
        /// <typeparam name="TUserClaimsPrincipalFactory"></typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IdentityBuilder AddClaimsTransform<TUserClaimsPrincipalFactory>(this IdentityBuilder builder) where TUserClaimsPrincipalFactory : class, IUserClaimsPrincipalFactory<User> {
            builder.Services.AddTransient<IUserClaimsPrincipalFactory<User>, TUserClaimsPrincipalFactory>();
            return builder;
        }
    }
}
