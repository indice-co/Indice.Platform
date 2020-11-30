using System;
using System.Linq;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Identity.Scopes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency injection configuration extensions.
    /// </summary>
    internal static class DynamicScopeConfiguration
    {
        /// <summary>
        /// Adds the scope metadata endpoint that will resolve the scope display name/description. Default configuration.
        /// </summary>
        /// <typeparam name="TScopeMetadataService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configureAction"></param>
        public static IIdentityServerBuilder AddDynamicScopeMetadataEndpoint<TScopeMetadataService>(this IIdentityServerBuilder builder, Action<DynamicScopeMetadataOptions> configureAction = null) where TScopeMetadataService : class, IDynamicScopeMetadataService {
            var existingService = builder.Services.Where(x => x.ServiceType == typeof(IDynamicScopeMetadataService)).LastOrDefault();
            if (existingService == null) {
                var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var options = new DynamicScopeMetadataOptions {
                    Endpoint = configuration["IdentityServer:ScopeMetadataEndpoint"]
                };
                configureAction?.Invoke(options);
                if (string.IsNullOrEmpty(options.Endpoint)) {
                    throw new Exception($"Configuration for {nameof(AddDynamicScopeMetadataEndpoint)} failed. Must provide a IdentityServer:ScopeMetadataEndpoint setting");
                }
                builder.Services.AddSingleton(options);
                builder.Services.AddHttpClient<IDynamicScopeMetadataService, TScopeMetadataService>()
                                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            }
            return builder;
        }

        /// <summary>
        /// Adds dynamic scope support to the current stores.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddDynamicScopes(this IIdentityServerBuilder builder) {
            return builder.AddClientStoreWithDynamicScopes()
                          .AddResourceStoreWithDynamicScopes()
                          .AddIntrospectionResponseGeneratorWithDynamicScopes();
        }

        /// <summary>
        /// Adds dynamic scope support to the current client store.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddClientStoreWithDynamicScopes(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IClientStore)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopeClientStore<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IClientStore), decoratorType);
            }
            return builder;
        }

        /// <summary>
        /// Adds dynamic scope support to the resource store.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddResourceStoreWithDynamicScopes(this IIdentityServerBuilder builder) {

            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IResourceStore)).LastOrDefault().ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopeResourceStore<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IResourceStore), decoratorType);
            }
            return builder;
        }

        /// <summary>
        /// Adds dynamic scope support to the current introspection response generator.
        /// </summary>
        /// <param name="builder">The builder.</param>
        public static IIdentityServerBuilder AddIntrospectionResponseGeneratorWithDynamicScopes(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IIntrospectionResponseGenerator)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopeIntrospectionResponseGenerator<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IIntrospectionResponseGenerator), decoratorType);
            }
            return builder;
        }

        /// <summary>
        /// Adds the required services in order to notify the consumer about scopes that have been revoked or granted.
        /// </summary>
        /// <typeparam name="TDynamicScopeNotificationService"></typeparam>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        /// <param name="configureAction">Configures options for <see cref="IDynamicScopeNotificationService"/>.</param>
        public static IIdentityServerBuilder AddDynamicScopeNotifications<TDynamicScopeNotificationService>(this IIdentityServerBuilder builder, Action<DynamicScopeNotificationOptions> configureAction = null)
            where TDynamicScopeNotificationService : class, IDynamicScopeNotificationService {
            var existingService = builder.Services.Where(x => x.ServiceType == typeof(IDynamicScopeNotificationService)).LastOrDefault();
            if (existingService == null) {
                var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var options = new DynamicScopeNotificationOptions {
                    Endpoint = configuration["IdentityServer:ScopeNotificationEndpoint"]
                };
                configureAction?.Invoke(options);
                if (string.IsNullOrEmpty(options.Endpoint)) {
                    throw new Exception($"Configuration for {nameof(AddDynamicScopeNotifications)} failed. Must provide a IdentityServer:ScopeNotificationEndpoint setting");
                }
                builder.Services.AddSingleton(options);
                builder.AddConsentServiceWithDynamicScopeNotifications();
                builder.AddPersistedGrantServiceWithDynamicScopeNotifications();
                builder.Services.AddHttpClient<IDynamicScopeNotificationService, TDynamicScopeNotificationService>()
                                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            }
            return builder;
        }

        /// <summary>
        /// Adds dynamic scope notifications support to the current consent service.
        /// </summary>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        private static IIdentityServerBuilder AddConsentServiceWithDynamicScopeNotifications(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IConsentService)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopeConsentService<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IConsentService), decoratorType);
            }
            return builder;
        }

        /// <summary>
        /// Adds dynamic scope notifications support to the current persisted grant service.
        /// </summary>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        private static IIdentityServerBuilder AddPersistedGrantServiceWithDynamicScopeNotifications(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IPersistedGrantService)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopePersistedGrantService<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IPersistedGrantService), decoratorType);
            }
            return builder;
        }
    }
}
