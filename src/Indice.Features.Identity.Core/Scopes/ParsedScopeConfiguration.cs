using System;
using System.Linq;
using IdentityServer4.Services;
using Indice.Features.Identity.Core.Scopes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency Injection Configuration extensions
    /// </summary>
    public static class ParsedScopeConfiguration
    {
        /// <summary>
        /// Adds the scope metadata endpoint that will resolve the scope display name/description. Default configuration.
        /// </summary>
        /// <typeparam name="TScopeMetadataService">The type of provided implementation of <see cref="IParsedScopeMetadataService"/>.</typeparam>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        /// <param name="configureAction">Configures options for <see cref="IParsedScopeMetadataService"/>.</param>
        public static IIdentityServerBuilder AddParsedScopeMetadataEndpoint<TScopeMetadataService>(this IIdentityServerBuilder builder, Action<ParsedScopeMetadataOptions> configureAction = null)
            where TScopeMetadataService : class, IParsedScopeMetadataService {
            var existingService = builder.Services.Where(x => x.ServiceType == typeof(IParsedScopeMetadataService)).LastOrDefault();
            if (existingService == null) {
                var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var options = new ParsedScopeMetadataOptions {
                    Endpoint = configuration["IdentityServer:ScopeMetadataEndpoint"]
                };
                configureAction?.Invoke(options);
                if (string.IsNullOrEmpty(options.Endpoint)) {
                    throw new Exception($"Configuration for {nameof(AddParsedScopeMetadataEndpoint)} failed. Must provide a IdentityServer:ScopeMetadataEndpoint setting.");
                }
                builder.Services.AddSingleton(options);
                builder.Services.AddHttpClient<IParsedScopeMetadataService, TScopeMetadataService>()
                                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            }
            return builder;
        }

        /// <summary>
        /// Adds the required services in order to notify the consumer about scopes that have been revoked or granted.
        /// </summary>
        /// <typeparam name="TParsedScopeNotificationService">The type of provided implementation of <see cref="IParsedScopeNotificationService"/>.</typeparam>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        /// <param name="configureAction">Configures options for <see cref="IParsedScopeNotificationService"/>.</param>
        public static IIdentityServerBuilder AddParsedScopeNotifications<TParsedScopeNotificationService>(this IIdentityServerBuilder builder, Action<ParsedScopeNotificationOptions> configureAction = null)
            where TParsedScopeNotificationService : class, IParsedScopeNotificationService {
            var existingService = builder.Services.Where(x => x.ServiceType == typeof(IParsedScopeNotificationService)).LastOrDefault();
            if (existingService == null) {
                var configuration = builder.Services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                var options = new ParsedScopeNotificationOptions {
                    Endpoint = configuration["IdentityServer:ScopeNotificationEndpoint"]
                };
                configureAction?.Invoke(options);
                if (string.IsNullOrEmpty(options.Endpoint)) {
                    throw new Exception($"Configuration for {nameof(AddParsedScopeNotifications)} failed. Must provide a IdentityServer:ScopeNotificationEndpoint setting.");
                }
                builder.Services.AddSingleton(options);
                builder.AddConsentServiceWithParsedScopeNotifications();
                builder.AddPersistedGrantServiceWithParsedScopeNotifications();
                builder.Services.AddHttpClient<IParsedScopeNotificationService, TParsedScopeNotificationService>()
                                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            }
            return builder;
        }

        /// <summary>
        /// Adds parsed scope notifications support to the current consent service.
        /// </summary>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        private static IIdentityServerBuilder AddConsentServiceWithParsedScopeNotifications(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IConsentService)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(ParsedScopeConsentService<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IConsentService), decoratorType);
            }
            return builder;
        }

        /// <summary>
        /// Adds parsed scope notifications support to the current persisted grant service.
        /// </summary>
        /// <param name="builder">The <see cref="IIdentityServerBuilder"/> builder.</param>
        private static IIdentityServerBuilder AddPersistedGrantServiceWithParsedScopeNotifications(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IPersistedGrantService)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(ParsedScopePersistedGrantService<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IPersistedGrantService), decoratorType);
            }
            return builder;
        }
    }
}
