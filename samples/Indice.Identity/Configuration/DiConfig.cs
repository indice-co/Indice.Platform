using System;
using FluentValidation.AspNetCore;
using Indice.AspNetCore.Identity.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides methods for configuring dependency injection.
    /// </summary>
    public static class DiConfig
    {
        /// <summary>
        /// Registers custom services to the ASP.NET Core's built-in dependency injection mechanism.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public static void AddCustomServices(this IServiceCollection services) {
            services.TryAddTransient<Func<ExtendedIdentityDbContext>>(provider => provider.GetService<ExtendedIdentityDbContext>);
            services.AddTransient<IValidatorInterceptor, ValidatorInterceptor>();
        }
    }
}
