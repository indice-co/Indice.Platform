using System;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        public static IMvcBuilder AddIdentityServerApiEndpoints(this IMvcBuilder mvcBuilder, Action<IdentityServerApiEndpointsOptions> configureAction = null) {
            mvcBuilder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new IdentityServerApiFeatureProvider()));
            var apiEndpointsOptions = new IdentityServerApiEndpointsOptions {
                Services = mvcBuilder.Services
            };
            // Initialize default options.
            configureAction?.Invoke(apiEndpointsOptions);
            apiEndpointsOptions.Services = null;
            var serviceProvider = mvcBuilder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            mvcBuilder.Services.AddDistributedMemoryCache();
            // Invoke action provided by developer to override default options.
            mvcBuilder.Services.AddSingleton(apiEndpointsOptions);
            mvcBuilder.Services.AddIndiceServices(configuration);
            mvcBuilder.Services.AddTransient<IEventService, EventService>();
            // Register validation filters.
            mvcBuilder.Services.AddScoped<CreateClaimTypeRequestValidationFilter>();
            mvcBuilder.Services.AddScoped<CreateRoleRequestValidationFilter>();
            // Add authorization policies that are used by the IdentityServer API.
            mvcBuilder.Services.AddAuthorization(authOptions => {
                authOptions.AddPolicy(IdentityServerApi.SubScopes.Users, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.SubScopes.Users) && (x.User.IsAdmin() || x.User.IsSystemClient()));
                });
                authOptions.AddPolicy(IdentityServerApi.SubScopes.Clients, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.SubScopes.Clients) && (x.User.IsAdmin() || x.User.IsSystemClient()));
                });
                authOptions.AddPolicy(IdentityServerApi.Admin, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.Scope) && (x.User.IsAdmin() || x.User.IsSystemClient()));
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
        /// Registers the DbContext to be used by the Identity system.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
        public static void AddDbContext(this IdentityServerApiEndpointsOptions options, Action<IdentityDbContextOptions> configureAction) {
            var contextOptions = new IdentityDbContextOptions();
            configureAction?.Invoke(contextOptions);
            options.Services.AddSingleton(contextOptions);
            if (contextOptions.ResolveDbContextOptions != null) {
                options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(contextOptions.ResolveDbContextOptions);
            } else {
                options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(contextOptions.ConfigureDbContext);
            }
            options.Services.AddTransient<Func<ExtendedIdentityDbContext<User, Role>>>(provider => provider.GetService<ExtendedIdentityDbContext<User, Role>>);
        }

        /// <summary>
        /// Registers an <see cref="IIdentityServerApiEventHandler{TEvent}"/> for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The implementation of <see cref="IIdentityServerApiEventHandler{TEvent}"/> to register.</typeparam>
        /// <typeparam name="THandler">The implementation of <see cref="IIdentityServerApiEventHandler{TEvent}"/> to register.</typeparam>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        public static void AddEventHandler<THandler, TEvent>(this IdentityServerApiEndpointsOptions options)
            where THandler : class, IIdentityServerApiEventHandler<TEvent>
            where TEvent : IIdentityServerApiEvent =>
            options.Services.AddTransient(typeof(IIdentityServerApiEventHandler<TEvent>), typeof(THandler));
    }
}
