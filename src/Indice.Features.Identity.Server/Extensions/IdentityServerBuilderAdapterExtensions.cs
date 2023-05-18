using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.Features.Identity.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Builder extension methods for IdentityServer4 forwarfing the method calls without loosing the reference to <see cref="IExtendedIdentityServerBuilder"/>
/// </summary>
public static class IdentityServerBuilderAdapterExtensions
{


    /// <summary>
    /// Adds the extension grant validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddExtensionGrantValidator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IExtensionGrantValidator {
        IdentityServerBuilderExtensionsAdditional.AddExtensionGrantValidator<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a redirect URI validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddRedirectUriValidator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IRedirectUriValidator {
        IdentityServerBuilderExtensionsAdditional.AddRedirectUriValidator<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a an "AppAuth" (OAuth 2.0 for Native Apps) compliant redirect URI validator (does strict validation but also allows http://127.0.0.1 with random port)
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddAppAuthRedirectUriValidator(this IExtendedIdentityServerBuilder builder) {
        IdentityServerBuilderExtensionsAdditional.AddAppAuthRedirectUriValidator(builder);
        return builder;
    }

    /// <summary>
    /// Adds the resource owner validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddResourceOwnerValidator<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, IResourceOwnerPasswordValidator {
        IdentityServerBuilderExtensionsAdditional.AddResourceOwnerValidator<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds the profile service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddProfileService<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, IProfileService {
        IdentityServerBuilderExtensionsAdditional.AddProfileService<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a resource validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddResourceValidator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IResourceValidator {
        IdentityServerBuilderExtensionsAdditional.AddResourceValidator<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a scope parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddScopeParser<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IScopeParser {
        IdentityServerBuilderExtensionsAdditional.AddScopeParser<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a client store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddClientStore<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, IClientStore {
        IdentityServerBuilderExtensionsAdditional.AddClientStore<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a resource store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddResourceStore<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, IResourceStore {
        IdentityServerBuilderExtensionsAdditional.AddResourceStore<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds a device flow store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    public static IExtendedIdentityServerBuilder AddDeviceFlowStore<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IDeviceFlowStore {
        IdentityServerBuilderExtensionsAdditional.AddDeviceFlowStore<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds a persisted grant store.
    /// </summary>
    /// <typeparam name="T">The type of the concrete grant store that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns>The builder.</returns>
    public static IExtendedIdentityServerBuilder AddPersistedGrantStore<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IPersistedGrantStore {
        IdentityServerBuilderExtensionsAdditional.AddPersistedGrantStore<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds a CORS policy service.
    /// </summary>
    /// <typeparam name="T">The type of the concrete scope store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddCorsPolicyService<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, ICorsPolicyService {
        IdentityServerBuilderExtensionsAdditional.AddCorsPolicyService<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a CORS policy service cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete CORS policy service that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddCorsPolicyCache<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, ICorsPolicyService {
        IdentityServerBuilderExtensionsAdditional.AddCorsPolicyCache<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds the secret parser.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddSecretParser<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, ISecretParser {
        IdentityServerBuilderExtensionsAdditional.AddSecretParser<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the secret validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddSecretValidator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, ISecretValidator {
        IdentityServerBuilderExtensionsAdditional.AddSecretValidator<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the client store cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete client store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddClientStoreCache<T>(this IExtendedIdentityServerBuilder builder)
        where T : IClientStore {
        IdentityServerBuilderExtensionsAdditional.AddClientStoreCache<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the client store cache.
    /// </summary>
    /// <typeparam name="T">The type of the concrete scope store class that is registered in DI.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddResourceStoreCache<T>(this IExtendedIdentityServerBuilder builder)
        where T : IResourceStore {
        IdentityServerBuilderExtensionsAdditional.AddResourceStoreCache<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds the authorize interaction response generator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddAuthorizeInteractionResponseGenerator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IAuthorizeInteractionResponseGenerator {
        IdentityServerBuilderExtensionsAdditional.AddAuthorizeInteractionResponseGenerator<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the custom authorize request validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddCustomAuthorizeRequestValidator<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, ICustomAuthorizeRequestValidator {
        IdentityServerBuilderExtensionsAdditional.AddCustomAuthorizeRequestValidator<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the custom authorize request validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddCustomTokenRequestValidator<T>(this IExtendedIdentityServerBuilder builder)
       where T : class, ICustomTokenRequestValidator {
        IdentityServerBuilderExtensionsAdditional.AddCustomTokenRequestValidator<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds support for client authentication using JWT bearer assertions.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddJwtBearerClientAuthentication(this IExtendedIdentityServerBuilder builder) {
        IdentityServerBuilderExtensionsAdditional.AddJwtBearerClientAuthentication(builder);

        return builder;
    }

    /// <summary>
    /// Adds a client configuration validator.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddClientConfigurationValidator<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IClientConfigurationValidator {
        IdentityServerBuilderExtensionsAdditional.AddClientConfigurationValidator<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds the X509 secret validators for mutual TLS.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddMutualTlsSecretValidators(this IExtendedIdentityServerBuilder builder) {
        IdentityServerBuilderExtensionsAdditional.AddMutualTlsSecretValidators(builder);

        return builder;
    }

    /// <summary>
    /// Adds a custom back-channel logout service.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddBackChannelLogoutService<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IBackChannelLogoutService {
        IdentityServerBuilderExtensionsAdditional.AddBackChannelLogoutService<T>(builder);

        return builder;
    }

    /// <summary>
    /// Adds a custom authorization request parameter store.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddAuthorizationParametersMessageStore<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IAuthorizationParametersMessageStore {
        IdentityServerBuilderExtensionsAdditional.AddAuthorizationParametersMessageStore<T>(builder);
        return builder;
    }

    /// <summary>
    /// Adds a custom user session.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="builder">The builder.</param>
    /// <returns></returns>
    public static IExtendedIdentityServerBuilder AddUserSession<T>(this IExtendedIdentityServerBuilder builder)
        where T : class, IUserSession {
        IdentityServerBuilderExtensionsAdditional.AddUserSession<T>(builder);
        return builder;
    }
}
