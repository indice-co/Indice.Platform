#if NET9_0_OR_GREATER
using Duende.IdentityServer.Infrastructure;
#else
using IdentityServer4.Infrastructure;
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Identity.Core.Configuration;

internal class ConfigureOAuthOptions<TOptions> : ConfigureExternalProviderOptions, IPostConfigureOptions<TOptions> where TOptions : OAuthOptions
{
    public ConfigureOAuthOptions(string[] schemes, IServiceProvider serviceProvider) : base(schemes, serviceProvider) {
    }

    void IPostConfigureOptions<TOptions>.PostConfigure(string? name, TOptions options) {
        PostConfigure(name, options);
    }
}
internal class ConfigureOpenIdOptions<TOptions> : ConfigureExternalProviderOptions, IPostConfigureOptions<TOptions> where TOptions : OpenIdConnectOptions
{
    public ConfigureOpenIdOptions(string[] schemes, IServiceProvider serviceProvider) : base(schemes, serviceProvider) {
    }

    void IPostConfigureOptions<TOptions>.PostConfigure(string? name, TOptions options) {
        PostConfigure(name, options);
    }
}

internal class ConfigureExternalProviderOptions : IPostConfigureOptions<OpenIdConnectOptions>, IPostConfigureOptions<OAuthOptions>
{
    private string[] _schemes;
    private readonly IServiceProvider _serviceProvider;

    public ConfigureExternalProviderOptions(string[] schemes, IServiceProvider serviceProvider) {
        _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    private static bool warnedInMemory;

    /// <inheritdoc/>
    public void PostConfigure(string? name, OpenIdConnectOptions options) {
        var stateformat = CreateSecureDataFormat(name);
        if (stateformat != null) {
            options.StateDataFormat = stateformat;
        }
    }

    public void PostConfigure(string? name, OAuthOptions options) {
        var stateformat = CreateSecureDataFormat(name);
        if (stateformat != null) {
            options.StateDataFormat = stateformat;
        }
    }

    private ISecureDataFormat<AuthenticationProperties>? CreateSecureDataFormat(string? name) {
        var secureDataFormat = default(ISecureDataFormat<AuthenticationProperties>);
        // no schemes means configure them all
        if (_schemes.Length == 0 || _schemes.Contains(name)) {
#if NET9_0_OR_GREATER
            secureDataFormat = new DistributedCacheStateDataFormatter(_serviceProvider, name);
#else
            secureDataFormat = new DistributedCacheStateDataFormatter(_serviceProvider.GetRequiredService<IHttpContextAccessor>(), name);
#endif
        }

        if (!warnedInMemory) {
            var distributedCacheService = _serviceProvider.GetRequiredService<IDistributedCache>();

            if (distributedCacheService is MemoryDistributedCache) {
                var logger = _serviceProvider
                    .GetRequiredService<ILogger<ConfigureExternalProviderOptions>>();

                logger.LogInformation("You have enabled the OidcStateDataFormatterCache but the distributed cache registered is the default memory based implementation. This will store any OIDC state in memory on the server that initiated the request. If the response is processed on another server it will fail. If you are running in production, you want to switch to a real distributed cache that is shared between all nodes.");

                warnedInMemory = true;
            }
        }

        return secureDataFormat;
    }
}