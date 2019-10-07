using System;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains extension methods on <see cref="IMvcBuilder"/> for configuring IdentityServer API feature.
    /// </summary>
    public static class IdentityServerApiFeatureExtensions
    {
        /// <summary>
        /// Adds the IdentityServer API endpoints to MVC.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration options for IdentityServer API feature.</param>
        public static IMvcBuilder AddIdentityServerApiEndpoints<TIdentityDbContext>(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null) where TIdentityDbContext : ExtendedIdentityDbContext {
            // Use the IdentityServerApiFeatureProvider to register IdentityServer API controllers.
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new IdentityServerApiFeatureProvider()));
            // Initialize default options.
            var options = new IdentityServerApiEndpointsOptions();
            // Invoke action provided by developer to override default options.
            configureAction?.Invoke(options);
            // Register option in DI mechanism for later use.
            mvcBuilder.Services.AddSingleton(options);
            mvcBuilder.Services.TryAddTransient<Func<TIdentityDbContext>>(provider => provider.GetService<TIdentityDbContext>);
            mvcBuilder.Services.AddTransient<IValidatorInterceptor, ValidatorInterceptor>();
            // Finally return the modified MVC builder.
            return mvcBuilder;
        }
    }
}
