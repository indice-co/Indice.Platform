#if NET9_0_OR_GREATER
using IdSrvModels = Duende.IdentityServer.Models;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.EntityFramework;
using Indice.Features.Identity.Core.TokenCleanup;
using Indice.Features.Identity.Core.Cache;
#else
using IdSrvModels = IdentityServer4.Models;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Options;
using IdentityServer4.Services;
#endif
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Events;
using Indice.Features.Identity.Core.Grants;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods to configure the IdentityServer.</summary>
public static class IdentityServerBuilderExtensions
{
    /// <summary>Setup an Event sink to filter login events and potentially log them into a persistent store like a db or a file.</summary>
    /// <typeparam name="TEventSink">The type of <see cref="IEventSink"/> implementation.</typeparam>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static IIdentityServerBuilder AddEventSink<TEventSink>(this IIdentityServerBuilder builder) where TEventSink : class, IEventSink {
        builder.Services.AddTransient<IEventSink, TEventSink>();
        return builder;
    }

    /// <summary>Adds support for token delegation.</summary>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static IIdentityServerBuilder AddDelegationGrantValidator(this IIdentityServerBuilder builder) {
        builder.AddExtensionGrantValidator<DelegationGrantValidator>();
        return builder;
    }

    /// <summary>Adds support for anonymous guest access tokens through the <c>urn:indice:guest</c> grant.</summary>
    /// <typeparam name="TIdentityServerBuilder">The type of the builder.</typeparam>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static TIdentityServerBuilder AddGuestGrantValidator<TIdentityServerBuilder>(this TIdentityServerBuilder builder) where TIdentityServerBuilder : IIdentityServerBuilder =>
        builder.AddGuestGrantValidator<TIdentityServerBuilder, GuestGrantValidator>();

    /// <summary>Adds support for anonymous guest access tokens through the <c>urn:indice:guest</c> grant, using a custom validator.</summary>
    /// <typeparam name="TIdentityServerBuilder">The type of the builder.</typeparam>
    /// <typeparam name="TValidator">A <see cref="GuestGrantValidator"/> subclass that overrides <c>GetClaimsAsync</c> to validate additional request data and enrich the issued claims.</typeparam>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static TIdentityServerBuilder AddGuestGrantValidator<TIdentityServerBuilder, TValidator>(this TIdentityServerBuilder builder)
        where TIdentityServerBuilder : IIdentityServerBuilder
        where TValidator : GuestGrantValidator {
        builder.Services.AddPushNotificationServiceNoop();
        builder.AddExtensionGrantValidator<TValidator>();
        return builder;
    }

    /// <summary>Registers <see cref="OtpAuthenticateExtensionGrantValidator"/> custom grant.</summary>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static IIdentityServerBuilder AddOtpAuthenticateGrantValidator(this IIdentityServerBuilder builder) {
        builder.AddExtensionGrantValidator<OtpAuthenticateExtensionGrantValidator>();
        return builder;
    }

    /// <summary>Adds a custom event handler to invalidate the client store cache when a client is created, updated or deleted.</summary>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    /// <returns>The builder for further configuration.</returns>
    public static IIdentityServerBuilder AddClientStoreCacheInvalidation(this IIdentityServerBuilder builder) {
        builder.Services.AddPlatformEventHandler<ClientUpdatedEvent, ClientCacheInvalidationEventHandler>();
        builder.Services.AddPlatformEventHandler<ClientDeletedEvent, ClientCacheInvalidationEventHandler>();
        return builder;
    }

    /// <summary>Adds an extended version of the built-in ResourceOwnerPasswordValidator, considering all the custom policies existing in the platform.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <typeparam name="TIdentityResourceOwnerPasswordValidator">The type of custom resource owner password validator.</typeparam>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    /// <param name="action">Configuration options for resource owner password grant.</param>
    public static IIdentityServerBuilder AddExtendedResourceOwnerPasswordValidator<TUser, TIdentityResourceOwnerPasswordValidator>(this IIdentityServerBuilder builder, Action<ResourceOwnerPasswordValidatorOptions>? action = null)
        where TUser : User
        where TIdentityResourceOwnerPasswordValidator : class, IResourceOwnerPasswordValidationFilter<TUser> {
        builder.Services.Configure<ResourceOwnerPasswordValidatorOptions>(options => action?.Invoke(options));
        builder.Services.AddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<ResourceOwnerPasswordValidatorOptions>>().Value);
        builder.Services.AddTransient<IResourceOwnerPasswordValidationFilter<TUser>, TIdentityResourceOwnerPasswordValidator>();
        builder.AddResourceOwnerValidator<ExtendedResourceOwnerPasswordValidator<TUser>>();
        var profileServiceImplementation = builder.Services.Where(x => x.ServiceType == typeof(IProfileService)).LastOrDefault()?.ImplementationType;
        if (profileServiceImplementation != null) {
            var decoratorType = typeof(ExtendedProfileService<>).MakeGenericType(profileServiceImplementation);
            builder.Services.TryAddTransient(profileServiceImplementation);
            builder.Services.AddTransient(typeof(IProfileService), decoratorType);
        }
        return builder;
    }

    /// <summary>Adds an extended version of the built-in ResourceOwnerPasswordValidator, considering all the custom policies existing in the platform.</summary>
    /// <typeparam name="TUser">The type of user.</typeparam>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    /// <param name="action">Configuration options for resource owner password grant.</param>
    public static IIdentityServerBuilder AddExtendedResourceOwnerPasswordValidator<TUser>(this IIdentityServerBuilder builder, Action<ResourceOwnerPasswordValidatorOptions>? action = null) where TUser : User =>
        builder.AddExtendedResourceOwnerPasswordValidator<TUser, IdentityResourceOwnerPasswordValidator<TUser>>(action);

    /// <summary>Adds an extended version of the built-in ResourceOwnerPasswordValidator, considering all the custom policies existing in the platform.</summary>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    /// <param name="action">Configuration options for resource owner password grant.</param>
    public static IIdentityServerBuilder AddExtendedResourceOwnerPasswordValidator(this IIdentityServerBuilder builder, Action<ResourceOwnerPasswordValidatorOptions>? action = null) => 
        builder.AddExtendedResourceOwnerPasswordValidator<User>(action);

    /// <summary>Setup configuration store.</summary>
    /// <param name="options">Options for configuring the configuration context.</param>
    public static void SetupTables(this ConfigurationStoreOptions options) {
        options.DefaultSchema = "config";
        options.ApiResource = new TableConfiguration(nameof(ApiResource));
        options.ApiResourceClaim = new TableConfiguration(nameof(ApiResourceClaim));
        options.ApiResourceProperty = new TableConfiguration(nameof(ApiResourceProperty));
        options.ApiResourceScope = new TableConfiguration(nameof(ApiResourceScope));
        options.ApiResourceSecret = new TableConfiguration(nameof(ApiResourceSecret));
        options.ApiScope = new TableConfiguration(nameof(ApiScope));
        options.ApiScopeClaim = new TableConfiguration(nameof(ApiScopeClaim));
        options.ApiScopeProperty = new TableConfiguration(nameof(ApiScopeProperty));
        options.Client = new TableConfiguration(nameof(Client));
        options.ClientClaim = new TableConfiguration(nameof(ClientClaim));
        options.ClientCorsOrigin = new TableConfiguration(nameof(ClientCorsOrigin));
        options.ClientGrantType = new TableConfiguration(nameof(ClientGrantType));
        options.ClientIdPRestriction = new TableConfiguration(nameof(ClientIdPRestriction));
        options.ClientPostLogoutRedirectUri = new TableConfiguration(nameof(ClientPostLogoutRedirectUri));
        options.ClientProperty = new TableConfiguration(nameof(ClientProperty));
        options.ClientRedirectUri = new TableConfiguration(nameof(ClientRedirectUri));
        options.ClientScopes = new TableConfiguration(nameof(ClientScope));
        options.ClientSecret = new TableConfiguration(nameof(ClientSecret));
        options.IdentityResource = new TableConfiguration(nameof(IdentityResource));
        options.IdentityResourceClaim = new TableConfiguration(nameof(IdentityResourceClaim));
        options.IdentityResourceProperty = new TableConfiguration(nameof(IdentityResourceProperty));
#if NET9_0_OR_GREATER
        options.IdentityProvider = new TableConfiguration(nameof(IdentityProvider));
#endif
    }

    /// <summary>Setup operational store.</summary>
    /// <param name="options">Options for configuring the operational context.</param>
    public static void SetupTables(this OperationalStoreOptions options) {
        options.DefaultSchema = "auth";
        options.PersistedGrants = new TableConfiguration(nameof(PersistedGrant));
        options.DeviceFlowCodes = new TableConfiguration(nameof(IdSrvModels.DeviceCode));
#if NET9_0_OR_GREATER
        options.ServerSideSessions = new TableConfiguration(nameof(IdSrvModels.ServerSideSession));
        options.Keys = new TableConfiguration(nameof(Key));
        options.PushedAuthorizationRequests = new TableConfiguration(nameof(PushedAuthorizationRequest));
#endif
    }

#if NET9_0_OR_GREATER
    /// <summary>
    /// Registers an alternative implementation of <see cref="TokenCleanupService"/>   
    /// that user an alternative way to delete records and removes events. 
    /// </summary>
    /// <param name="builder">instance</param>
    /// <returns>The current <see cref="IIdentityServerBuilder"/> instance.</returns>
    public static TIdentityServerBuilder AddFastCleanUpService<TIdentityServerBuilder>(this TIdentityServerBuilder builder) where TIdentityServerBuilder : IIdentityServerBuilder {
        builder.Services.AddTransient<ITokenCleanupService, FastTokenCleanupService>();
        return builder;
    }

    /// <summary>
    /// Registers an alternative implementation of <see cref="ICache{T}"/> using <c>HybridCache</c>
    /// </summary>
    /// <param name="builder">instance</param>
    /// <returns>The current <see cref="IIdentityServerBuilder"/> instance.</returns>
    public static TIdentityServerBuilder AddHybridCache<TIdentityServerBuilder>(this TIdentityServerBuilder builder) where TIdentityServerBuilder : IIdentityServerBuilder {
        // Add HybridCache service
        builder.Services.AddHybridCache();
        builder.Services.AddTransient(typeof(ICache<>), typeof(DuendeHybridCache<>));
        return builder;
    }
#endif
}
