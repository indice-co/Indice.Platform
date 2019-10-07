using System;
using FluentValidation.AspNetCore;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
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
        public static IMvcBuilder AddIdentityServerApiEndpoints<TIdentityDbContext>(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null)
            where TIdentityDbContext : ExtendedIdentityDbContext<User, Role> {
            var genericArguments = typeof(TIdentityDbContext).GetGenericArguments();
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new IdentityServerApiFeatureProvider())); // Use the IdentityServerApiFeatureProvider to register IdentityServer API controllers.
            var options = new IdentityServerApiEndpointsOptions(); // Initialize default options.
            configureAction?.Invoke(options); // Invoke action provided by developer to override default options.
            mvcBuilder.Services.AddSingleton(options); // Register option in DI mechanism for later use.
            mvcBuilder.Services.TryAddTransient<Func<TIdentityDbContext>>(provider => provider.GetService<TIdentityDbContext>); // Used extensively in validators.
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
    }
}
