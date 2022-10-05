using System;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.Events;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Endpoints;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Services;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Contains methods for configuring the trusted device authorization feature.</summary>
    public static class TrustedDeviceAuthorizationConfiguration
    {
        /// <summary>Register the endpoints and required services for trusted device authorization.</summary>
        /// <param name="builder">IdentityServer builder interface.</param>
        /// <param name="configureAction"></param>
        public static IIdentityServerBuilder AddTrustedDeviceAuthorization(this IIdentityServerBuilder builder, Action<TrustedDeviceAuthorizationOptions> configureAction = null) {
            var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var options = new TrustedDeviceAuthorizationOptions {
                Services = builder.Services,
                Configuration = configuration
            };
            configureAction?.Invoke(options);
            // Register endpoints.
            builder.RegisterEndpoints();
            // Register stores and services.
            builder.Services.AddTransient<IAuthorizationCodeChallengeStore, DefaultAuthorizationCodeChallengeStore>();
            builder.Services.TryAddTransient<IPlatformEventService, PlatformEventService>();
            builder.Services.TryAddScoped<IdentityMessageDescriber>();
            options.AddUserDeviceStoreInMemory();
            // Register custom grant validator.
            builder.AddExtensionGrantValidator<TrustedDeviceExtensionGrantValidator>();
            // Register core services.
            options.AddDefaultPasswordHasher();
            options.RegisterCoreServices();
            // Register events.
            options.RegisterEvents();
            return builder;
        }

        /// <summary>Adds an in-memory implementation for the <see cref="IUserDeviceStore"/> store.</summary>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddUserDeviceStoreInMemory(this TrustedDeviceAuthorizationOptions options) =>
            options.Services.TryAddSingleton<IUserDeviceStore, UserDeviceStoreInMemory>();

        /// <summary>Add an implementation of <see cref="IUserDeviceStore"/> for persisting user devices in a relational database using Entity Framework Core.</summary>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddUserDeviceStoreEntityFrameworkCore(this TrustedDeviceAuthorizationOptions options) =>
            options.AddUserDeviceStore<UserDeviceStoreEntityFrameworkCore>();

        /// <summary>Adds a custom implementation for <see cref="IUserDeviceStore"/> store.</summary>
        /// <typeparam name="TUserDeviceStore">The type of <see cref="UserDevice"/> store.</typeparam>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddUserDeviceStore<TUserDeviceStore>(this TrustedDeviceAuthorizationOptions options) where TUserDeviceStore : class, IUserDeviceStore =>
            options.Services.AddTransient<IUserDeviceStore, TUserDeviceStore>();

        /// <summary>Adds the default hashing mechanism for devices.</summary>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddDefaultPasswordHasher(this TrustedDeviceAuthorizationOptions options) {
            options.Services.TryAddTransient<IDevicePasswordHasher, DefaultDevicePasswordHasher>();
            options.Services.TryAddScoped<PasswordHasher<User>>();
        }

        /// <summary>Registers an implementation of the mechanism that performs password hashing and validation for devices.</summary>
        /// <typeparam name="TDevicePasswordHasher">The type of <see cref="IDevicePasswordHasher"/> implementation to register.</typeparam>
        /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
        public static void AddDevicePasswordHasher<TDevicePasswordHasher>(this TrustedDeviceAuthorizationOptions options) where TDevicePasswordHasher : IDevicePasswordHasher =>
            options.Services.AddTransient(typeof(IDevicePasswordHasher), typeof(TDevicePasswordHasher));

        private static void RegisterCoreServices(this TrustedDeviceAuthorizationOptions options) {
            options.Services.AddTransient<BearerTokenUsageValidator>();
            options.Services.AddTransient<CompleteRegistrationRequestValidator>();
            options.Services.AddTransient<CompleteRegistrationResponseGenerator>();
            options.Services.AddTransient<DeviceAuthorizationRequestValidator>();
            options.Services.AddTransient<DeviceAuthorizationResponseGenerator>();
            options.Services.AddTransient<InitRegistrationRequestValidator>();
            options.Services.AddTransient<InitRegistrationResponseGenerator>();
            options.Services.AddTransient<IResourceOwnerPasswordValidationFilter<User>, DeviceResourceOwnerPasswordValidator<User>>();
        }

        private static void RegisterEndpoints(this IIdentityServerBuilder builder) {
            builder.AddEndpoint<InitRegistrationEndpoint>("TrustedDeviceInitRegistration", "/my/devices/register/init");
            builder.AddEndpoint<CompleteRegistrationEndpoint>("TrustedDeviceCompleteRegistration", "/my/devices/register/complete");
            builder.AddEndpoint<DeviceAuthorizationEndpoint>("TrustedDeviceAuthorization", "/my/devices/connect/authorize");
        }

        private static void RegisterEvents(this TrustedDeviceAuthorizationOptions options) {
            var requireCredentialsOnAccountChange = options.Configuration.GetValue<bool?>("IdentityOptions:User:Devices:RequireCredentialsOnAccountChange") ??
                                                    options.Configuration.GetValue<bool?>("User:Devices:RequireCredentialsOnAccountChange") ??
                                                    false;
            if (requireCredentialsOnAccountChange) {
                options.Services.AddPlatformEventHandler<UserNameChangedEvent, UserNameOrPasswordChangedEventHandler>();
                options.Services.AddPlatformEventHandler<PasswordChangedEvent, UserNameOrPasswordChangedEventHandler>();
            }
        }
    }
}