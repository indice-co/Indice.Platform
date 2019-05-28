using System;
using System.Linq;
using Indice.AspNetCore.Identity.Scopes;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Dependency Injection Configuration extensions
    /// </summary>
    public static class DynamicScopeConfiguration
    {
        /// <summary>
        /// Adds the scope metadata endpoint that will resolve the scope displayname/description. Default configuration.
        /// </summary>
        /// <typeparam name="TScopeMetadataService"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configureAction"></param>
        /// <returns></returns>
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
        /// Adds dynamic scope support to the current stores
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddDynamicScopes(this IIdentityServerBuilder builder) {
            return builder.AddClientStoreWithDynamicScopes()
                          .AddResourceStoreWithDynamicScopes()
                          .AddIntrospectionResponseGeneratorWithDynamicScopes();
        }

        /// <summary>
        /// Adds dynamic scope support to the current client store 
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// Adds dynamic scope support to the current introspection response generator
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddIntrospectionResponseGeneratorWithDynamicScopes(this IIdentityServerBuilder builder) {
            var implementation = builder.Services.Where(x => x.ServiceType == typeof(IIntrospectionResponseGenerator)).LastOrDefault()?.ImplementationType;
            if (implementation != null) {
                var decoratorType = typeof(DynamicScopeIntrospectionResponseGenerator<>).MakeGenericType(implementation);
                builder.Services.TryAddTransient(implementation);
                builder.Services.AddTransient(typeof(IIntrospectionResponseGenerator), decoratorType);
            }
            return builder;
        }
    }
}
