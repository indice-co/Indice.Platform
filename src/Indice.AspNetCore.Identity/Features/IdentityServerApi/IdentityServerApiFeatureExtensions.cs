using System;
using FluentValidation.AspNetCore;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains extension methods on <see cref="IMvcBuilder"/> for configuring IdentityServer API feature.
    /// </summary>
    public static class IdentityServerApiFeatureExtensions
    {
        /// <summary>
        /// Adds the IdentityServer API endpoints to MVC with default user and role types.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration options for IdentityServer API feature.</param>
        /// <returns></returns>
        public static IMvcBuilder AddIdentityServerApiEndpoints(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null) =>
            mvcBuilder.AddIdentityServerApiEndpoints<User, Role>(configureAction);

        /// <summary>
        /// Adds the IdentityServer API endpoints to MVC.
        /// </summary>
        /// <param name="mvcBuilder">An interface for configuring MVC services.</param>
        /// <param name="configureAction">Configuration options for IdentityServer API feature.</param>
        public static IMvcBuilder AddIdentityServerApiEndpoints<TUser, TRole>(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null)
            where TUser : User, new()
            where TRole : Role, new() {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new IdentityServerApiFeatureProvider())); // Use the IdentityServerApiFeatureProvider to register IdentityServer API controllers.
            var options = new IdentityServerApiEndpointsOptions {
                Services = mvcBuilder.Services
            };
            // Initialize default options.
            configureAction?.Invoke(options); // Invoke action provided by developer to override default options.
            options.Services = null;
            mvcBuilder.Services.AddSingleton(options); // Register option in DI mechanism for later use.
            mvcBuilder.Services.AddTransient<Func<ExtendedIdentityDbContext<TUser, TRole>>>(provider => provider.GetService<ExtendedIdentityDbContext<TUser, TRole>>); // Used extensively in validators.
            mvcBuilder.Services.AddTransient<IValidatorInterceptor, ValidatorInterceptor>();
            // Add authorization policies that are used by the IdentityServer API.
            mvcBuilder.Services.AddAuthorization(options => {
                options.AddPolicy(IdentityServerApi.SubScopes.Users, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.SubScopes.Users) || x.User.IsAdmin());
                });
                options.AddPolicy(IdentityServerApi.SubScopes.Clients, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.SubScopes.Clients) || x.User.IsAdmin());
                });
                options.AddPolicy(IdentityServerApi.Admin, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.IsAdmin());
                });
            });
            // Register the authentication handler, using a custom scheme name, for local APIs.
            mvcBuilder.Services.AddAuthentication()
                               .AddLocalApi(IdentityServerApi.AuthenticationScheme, options => {
                                   options.ExpectedScope = IdentityServerApi.Scope;
                               });
            return mvcBuilder;
        }

        /// <summary>
        /// Registers the DbContext to be used by the Identity system with default user and role types..
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
        public static void AddDbContext(this IdentityServerApiEndpointsOptions options, Action<IdentityDbContextOptions> configureAction) => 
            options.AddDbContext<User, Role>(configureAction);

        /// <summary>
        /// Registers the DbContext to be used by the Identity system.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
        public static void AddDbContext<TUser, TRole>(this IdentityServerApiEndpointsOptions options, Action<IdentityDbContextOptions> configureAction)
            where TUser : User, new()
            where TRole : Role, new() {
            var contextOptions = new IdentityDbContextOptions();
            configureAction?.Invoke(contextOptions);
            options.Services.AddSingleton(contextOptions);
            if (contextOptions.ResolveDbContextOptions != null) {
                options.Services.AddDbContext<ExtendedIdentityDbContext<TUser, TRole>>(contextOptions.ResolveDbContextOptions);
            } else {
                options.Services.AddDbContext<ExtendedIdentityDbContext<TUser, TRole>>(contextOptions.ConfigureDbContext);
            }
        }
    }
}
