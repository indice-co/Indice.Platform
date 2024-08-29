﻿using System;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Options;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Features.Identity.Core.Grants;
using Indice.Features.Identity.Core.ResponseHandling;
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

    /// <summary>Registers <see cref="OtpAuthenticateExtensionGrantValidator"/> custom grant.</summary>
    /// <param name="builder"><see cref="IIdentityServerBuilder"/> builder interface.</param>
    public static IIdentityServerBuilder AddOtpAuthenticateGrantValidator(this IIdentityServerBuilder builder) {
        builder.AddExtensionGrantValidator<OtpAuthenticateExtensionGrantValidator>();
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
    }

    /// <summary>Setup operational store.</summary>
    /// <param name="options">Options for configuring the operational context.</param>
    public static void SetupTables(this OperationalStoreOptions options) {
        options.DefaultSchema = "auth";
        options.PersistedGrants = new TableConfiguration(nameof(PersistedGrant));
        options.DeviceFlowCodes = new TableConfiguration(nameof(IdentityServer4.Models.DeviceCode));
    }
}
