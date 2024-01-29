using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Mvc.Authorization;

internal class CampaignsPolicyProvider(
    IOptions<AuthorizationOptions> options,
    IOptions<MessageManagementOptions> apiOptions
    ) : IAuthorizationPolicyProvider
{
    private readonly MessageManagementOptions _apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));

    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; } = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName) {
        if (policyName.Equals(MessagesApi.Policies.BeCampaignManager, StringComparison.OrdinalIgnoreCase)) {
            var policy = new AuthorizationPolicyBuilder();
            policy.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                  .RequireAuthenticatedUser()
                  .RequireAssertion(context => 
                     context.User.HasScope(_apiOptions.RequiredScope ?? MessagesApi.Scope) && 
                     (context.User.IsAdmin() || context.User.HasRoleClaim(BasicRoleNames.CampaignManager))
                  );
            return Task.FromResult(policy.Build());
        }
        return FallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
