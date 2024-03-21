using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Media.AspNetCore.Authorization;
internal class MediaLibraryPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly MediaApiOptions _apiOptions;

    public MediaLibraryPolicyProvider(
        IOptions<AuthorizationOptions> options,
        IOptions<MediaApiOptions> apiOptions
    ) {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        _apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
    }

    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName) {
        if (policyName.Equals(MediaLibraryApi.Policies.BeMediaLibraryManager, StringComparison.OrdinalIgnoreCase)) {
            var policy = new AuthorizationPolicyBuilder();
            policy.RequireMediaLibraryManager(_apiOptions);
            return Task.FromResult(policy?.Build());
        }
        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Extensions on the <see cref="AuthorizationPolicyBuilder"/>
/// </summary>
public static class MediaLibraryAuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// Adds enforces that the current user is authenticated. Enforces api scope to be present for <see cref="MediaApiOptions.ApiScope"/>. And works only under the specified <see cref="MediaApiOptions.AuthenticationScheme"/>.
    /// </summary>
    /// <returns>A reference to the <paramref name="policyBuilder"/> instance after the operation has completed.</returns>
    public static AuthorizationPolicyBuilder RequireMediaLibraryManager(this AuthorizationPolicyBuilder policyBuilder, MediaApiOptions mediaApiOptions) {
        policyBuilder.AddAuthenticationSchemes(mediaApiOptions.AuthenticationScheme ?? MediaLibraryApi.AuthenticationScheme)
                 .RequireAuthenticatedUser()
                 .RequireAssertion(context => context.User.HasScope(mediaApiOptions.ApiScope ?? MediaLibraryApi.Scope) || context.User.IsSystemClient() || context.User.IsAdmin());
        return policyBuilder;
    }
}
