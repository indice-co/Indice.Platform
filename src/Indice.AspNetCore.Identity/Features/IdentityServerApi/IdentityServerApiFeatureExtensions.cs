using System;
using IdentityModel;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Api;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.Configuration;
using Indice.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;

namespace Microsoft.Extensions.DependencyInjection
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
            var services = mvcBuilder.Services;
            var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var apiEndpointsOptions = new IdentityServerApiEndpointsOptions {
                Services = services
            };
            // Initialize default options.
            configureAction?.Invoke(apiEndpointsOptions);
            apiEndpointsOptions.Services = null;
            services.AddDistributedMemoryCache();
            services.AddFeatureManagement(configuration.GetSection("IdentityServerApiFeatureManagement"));
            // Configure options for CacheResourceFilter.
            services.Configure<CacheResourceFilterOptions>(options => options.DisableCache = apiEndpointsOptions.DisableCache);
            // Invoke action provided by developer to override default options.
            services.AddSingleton(apiEndpointsOptions);
            services.AddIndiceServices(configuration);
            services.AddTransient<IEventService, EventService>();
            // Register validation filters.
            services.AddScoped<CreateClaimTypeRequestValidationFilter>();
            services.AddScoped<CreateRoleRequestValidationFilter>();
            // Add authorization policies that are used by the IdentityServer API.
            services.AddAuthorization(authOptions => {
                authOptions.AddPolicy(IdentityServerApi.Scope, policy => {
                    policy.AddAuthenticationSchemes(IdentityServerApi.AuthenticationScheme)
                          .RequireAuthenticatedUser()
                          .RequireAssertion(x => x.User.HasClaim(JwtClaimTypes.Scope, IdentityServerApi.Scope) || x.User.IsAdmin() || x.User.IsSystemClient());
                });
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
            // Configure antiforgery token options.
            services.Configure<AntiforgeryOptions>(options => {
                options.HeaderName = CustomHeaderNames.AntiforgeryHeaderName;
            });
            services.TryAddScoped<IdentityMessageDescriber>();
            // Register the authentication handler, using a custom scheme name, for local APIs.
            services.AddAuthentication()
                    .AddLocalApi(IdentityServerApi.AuthenticationScheme, options => {
                        options.ExpectedScope = IdentityServerApi.Scope;
                    });
            return mvcBuilder;
        }

        /// <summary>
        /// Registers the <see cref="DbContext"/> to be used by the Identity system.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.</param>
        public static void AddDbContext(this IdentityServerApiEndpointsOptions options, Action<IdentityDbContextOptions> configureAction) {
            var dbContextOptions = new IdentityDbContextOptions();
            configureAction?.Invoke(dbContextOptions);
            options.Services.AddSingleton(dbContextOptions);
            if (dbContextOptions.ResolveDbContextOptions != null) {
                options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(dbContextOptions.ResolveDbContextOptions);
            } else {
                options.Services.AddDbContext<ExtendedIdentityDbContext<User, Role>>(dbContextOptions.ConfigureDbContext);
            }
        }

        /// <summary>
        /// Registers an implementation of <see cref="IIdentityServerApiEventHandler{TEvent}"/> for the specified event type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event to handler.</typeparam>
        /// <typeparam name="TEventHandler">The handler to user for the specified event.</typeparam>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        public static IdentityServerApiEndpointsOptions AddEventHandler<TEvent, TEventHandler>(this IdentityServerApiEndpointsOptions options)
            where TEvent : IIdentityServerApiEvent
            where TEventHandler : class, IIdentityServerApiEventHandler<TEvent> {
            options.Services.AddTransient(typeof(IIdentityServerApiEventHandler<TEvent>), typeof(TEventHandler));
            return options;
        }
    }
}
