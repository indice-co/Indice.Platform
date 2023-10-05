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
            policy.AddAuthenticationSchemes(_apiOptions.AuthenticationScheme ?? MediaLibraryApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(context => context.User.HasScope(_apiOptions.ApiScope ?? MediaLibraryApi.Scope) || context.User.IsSystemClient() || context.User.IsAdmin());
            return Task.FromResult(policy?.Build());
        }
        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
