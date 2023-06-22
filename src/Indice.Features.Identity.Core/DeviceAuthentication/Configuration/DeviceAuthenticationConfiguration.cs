using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Endpoints;
using Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;
using Indice.Features.Identity.Core.DeviceAuthentication.Services;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Grants;
using Indice.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Contains methods for configuring the trusted device authorization feature.</summary>
public static class DeviceAuthenticationConfiguration
{
    /// <summary>Register the endpoints and required services for trusted device authorization.</summary>
    /// <param name="builder">IdentityServer builder interface.</param>
    /// <param name="configureAction"></param>
    public static IIdentityServerBuilder AddDeviceAuthentication(this IIdentityServerBuilder builder, Action<DeviceAuthenticationOptions> configureAction = null) {
        var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var options = new DeviceAuthenticationOptions {
            Services = builder.Services,
            Configuration = configuration
        };
        configureAction?.Invoke(options);
        // Register endpoints.
        builder.RegisterEndpoints();
        // Register stores and services.
        builder.Services.AddTransient<IDeviceAuthenticationCodeChallengeStore, DefaultDeviceAuthenticationCodeChallengeStore>();
        builder.Services.TryAddTransient<IPlatformEventService, PlatformEventService>();
        builder.Services.TryAddScoped<IdentityMessageDescriber>();
        options.AddUserDeviceStoreInMemory();
        // Register custom grant validator.
        builder.AddExtensionGrantValidator<DeviceAuthenticationExtensionGrantValidator>();
        // Register core services.
        options.AddDefaultPasswordHasher();
        options.RegisterCoreServices();
        // Register events.
        options.RegisterEvents();
        return builder;
    }

    /// <summary>Adds an in-memory implementation for the <see cref="IUserDeviceStore"/> store.</summary>
    /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
    public static void AddUserDeviceStoreInMemory(this DeviceAuthenticationOptions options) =>
        options.Services.TryAddSingleton<IUserDeviceStore, UserDeviceStoreInMemory>();

    /// <summary>Add an implementation of <see cref="IUserDeviceStore"/> for persisting user devices in a relational database using Entity Framework Core.</summary>
    /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
    public static void AddUserDeviceStoreEntityFrameworkCore(this DeviceAuthenticationOptions options) =>
        options.AddUserDeviceStore<UserDeviceStoreEntityFrameworkCore>();

    /// <summary>Adds a custom implementation for <see cref="IUserDeviceStore"/> store.</summary>
    /// <typeparam name="TUserDeviceStore">The type of <see cref="UserDevice"/> store.</typeparam>
    /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
    public static void AddUserDeviceStore<TUserDeviceStore>(this DeviceAuthenticationOptions options) where TUserDeviceStore : class, IUserDeviceStore =>
        options.Services.AddTransient<IUserDeviceStore, TUserDeviceStore>();

    /// <summary>Adds the default hashing mechanism for devices.</summary>
    /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
    public static void AddDefaultPasswordHasher(this DeviceAuthenticationOptions options) {
        options.Services.TryAddTransient<IDevicePasswordHasher, DefaultDevicePasswordHasher>();
        options.Services.TryAddScoped<PasswordHasher<User>>();
    }

    /// <summary>Registers an implementation of the mechanism that performs password hashing and validation for devices.</summary>
    /// <typeparam name="TDevicePasswordHasher">The type of <see cref="IDevicePasswordHasher"/> implementation to register.</typeparam>
    /// <param name="options">Options for configuring 'Trusted Device Authorization' feature.</param>
    public static void AddDevicePasswordHasher<TDevicePasswordHasher>(this DeviceAuthenticationOptions options) where TDevicePasswordHasher : IDevicePasswordHasher =>
        options.Services.AddTransient(typeof(IDevicePasswordHasher), typeof(TDevicePasswordHasher));

    private static void RegisterCoreServices(this DeviceAuthenticationOptions options) {
        options.Services.AddTransient<BearerTokenUsageValidator>();
        options.Services.AddTransient<CompleteRegistrationRequestValidator>();
        options.Services.AddTransient<CompleteRegistrationResponseGenerator>();
        options.Services.AddTransient<DeviceAuthenticationRequestValidator>();
        options.Services.AddTransient<DeviceAuthenticationResponseGenerator>();
        options.Services.AddTransient<InitRegistrationRequestValidator>();
        options.Services.AddTransient<InitRegistrationResponseGenerator>();
        options.Services.AddTransient<IResourceOwnerPasswordValidationFilter<User>, DeviceResourceOwnerPasswordValidator<User>>();
    }

    private static void RegisterEndpoints(this IIdentityServerBuilder builder) {
        builder.AddEndpoint<InitRegistrationEndpoint>("TrustedDeviceInitRegistration", "/my/devices/register/init");
        builder.AddEndpoint<CompleteRegistrationEndpoint>("TrustedDeviceCompleteRegistration", "/my/devices/register/complete");
        builder.AddEndpoint<DeviceAuthenticationEndpoint>("TrustedDeviceAuthorization", "/my/devices/connect/authorize");
    }

    private static void RegisterEvents(this DeviceAuthenticationOptions options) {
        var requirePasswordAfterUserUpdate = options.Configuration.GetValue<bool?>("IdentityOptions:User:Devices:RequirePasswordAfterUserUpdate") ??
                                             options.Configuration.GetValue<bool?>("User:Devices:RequirePasswordAfterUserUpdate") ??
                                             false;
        if (requirePasswordAfterUserUpdate) {
            options.Services.AddPlatformEventHandler<UserNameChangedEvent, UserNameOrPasswordChangedEventHandler>();
            options.Services.AddPlatformEventHandler<PasswordChangedEvent, UserNameOrPasswordChangedEventHandler>();
        }
    }
}
