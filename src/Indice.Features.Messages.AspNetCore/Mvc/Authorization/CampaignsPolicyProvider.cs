using System;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Mvc.Authorization
{
    internal class CampaignsPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly MessageManagementOptions _apiOptions;

        public CampaignsPolicyProvider(
            IOptions<AuthorizationOptions> options,
            IOptions<MessageManagementOptions> apiOptions
        ) {
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
            _apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
        }

        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName) {
            if (policyName.Equals(MessagesApi.Policies.BeCampaignManager, StringComparison.OrdinalIgnoreCase)) {
                var policy = new AuthorizationPolicyBuilder();
                policy.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(context => context.User.HasScope(_apiOptions.RequiredScope ?? MessagesApi.Scope));
                return Task.FromResult(policy.Build());
            }
            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
