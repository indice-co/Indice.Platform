using System;
using IdentityModel;
using Indice.AspNetCore.Identity.Models;
using Indice.AspNetCore.Identity.Services;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            // Invoke action provided by developer to override default options.
            services.AddSingleton(apiEndpointsOptions);
            services.AddIndiceServices(configuration);
            services.AddTransient<IEventService, EventService>();
            // Register validation filters.
            services.AddScoped<CreateClaimTypeRequestValidationFilter>();
            services.AddScoped<CreateRoleRequestValidationFilter>();
            // Add authorization policies that are used by the IdentityServer API.
            services.AddAuthorization(authOptions => {
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
            // Try register the extended version of UserManager<User>.
            services.TryAddScoped<ExtendedUserManager<User>>();
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
            options.Services.AddTransient<Func<ExtendedIdentityDbContext<User, Role>>>(provider => provider.GetService<ExtendedIdentityDbContext<User, Role>>);
        }

        /// <summary>
        /// Configures the parameters needed when a verification email is sent to the user.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for the email sent to user for verification.</param>
        public static void ConfigureEmailVerification(this IdentityServerApiEndpointsOptions options, Action<EmailVerificationOptions> configureAction) {
            var userEmailVerificationOptions = new EmailVerificationOptions();
            configureAction?.Invoke(userEmailVerificationOptions);
            options.Services.AddSingleton(userEmailVerificationOptions);
        }

        /// <summary>
        /// Configures the parameters needed when an email is sent to the user when the email address is changed.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for the email sent to user.</param>
        public static void ConfigureChangeEmail(this IdentityServerApiEndpointsOptions options, Action<ChangeEmailOptions> configureAction) {
            var changeEmailOptions = new ChangeEmailOptions();
            configureAction?.Invoke(changeEmailOptions);
            options.Services.AddSingleton(changeEmailOptions);
        }

        /// <summary>
        /// Configures the parameters needed when an SMS is sent to the user when the phone number is changed.
        /// </summary>
        /// <param name="options">Options for configuring the IdentityServer API feature.</param>
        /// <param name="configureAction">Configuration for the SMS sent to user.</param>
        public static void ConfigureChangePhone(this IdentityServerApiEndpointsOptions options, Action<ChangePhoneNumberOptions> configureAction) {
            var changePhoneNumberOptions = new ChangePhoneNumberOptions();
            configureAction?.Invoke(changePhoneNumberOptions);
            options.Services.AddSingleton(changePhoneNumberOptions);
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
