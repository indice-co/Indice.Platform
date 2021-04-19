using System;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for configuring the trusted device authorization feature.
    /// </summary>
    public static class TrustedDeviceAuthorizationConfiguration
    {
        /// <summary>
        /// Register the endpoints and required services for trusted device authorization.
        /// </summary>
        /// <param name="builder">IdentityServer builder interface.</param>
        /// <param name="configureAction"></param>
        public static IIdentityServerBuilder AddTrustedDeviceAuthorization(this IIdentityServerBuilder builder, Action<TrustedDeviceAuthorizationOptions> configureAction = null) {
            var options = new TrustedDeviceAuthorizationOptions {
                Services = builder.Services
            };
            configureAction?.Invoke(options);
            // Register endpoints.
            builder.AddEndpoint<InitRegistrationEndpoint>("TrustedDeviceInitRegistration", "/my/devices/register/init");
            builder.AddEndpoint<CompleteRegistrationEndpoint>("TrustedDeviceCompleteRegistration", "/my/devices/register/complete");
            // Register stores.
            builder.Services.AddTransient<IAuthorizationCodeChallengeStore, DefaultAuthorizationCodeChallengeStore>();
            options.AddInMemoryUserDeviceStore();
            // Register other services.
            builder.Services.AddTransient<BearerTokenUsageValidator>();
            builder.Services.AddTransient<InitRegistrationRequestValidator>();
            builder.Services.AddTransient<InitRegistrationResponseGenerator>();
            builder.Services.AddTransient<CompleteRegistrationRequestValidator>();
            builder.Services.AddTransient<CompleteRegistrationResponseGenerator>();
            return builder;
        }

        /// <summary>
        /// Adds an in-memory implementation for the <see cref="IUserDeviceStore"/> store.
        /// </summary>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddInMemoryUserDeviceStore(this TrustedDeviceAuthorizationOptions options) => options.Services.TryAddSingleton<IUserDeviceStore, InMemoryUserDeviceStore>();

        /// <summary>
        /// Adds a custom implementation for <see cref="IUserDeviceStore"/> store.
        /// </summary>
        /// <typeparam name="TUserDeviceStore">The type of <see cref="UserDevice"/> store.</typeparam>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddUserDeviceStore<TUserDeviceStore>(this TrustedDeviceAuthorizationOptions options) where TUserDeviceStore : class, IUserDeviceStore {
            options.Services.AddTransient<IUserDeviceStore, TUserDeviceStore>();
        }
    }
}
