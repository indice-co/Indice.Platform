using System;
using System.Threading.Tasks;
using Indice.Features.Messages.Core;
using Indice.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Mvc.Authorization
{
    internal class MultitenantCampaignsPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly MessageManagementOptions _apiOptions;
        private readonly MessageMultitenancyOptions _multitenancyOptions;

        public MultitenantCampaignsPolicyProvider(
            IOptions<AuthorizationOptions> options,
            IOptions<MessageManagementOptions> apiOptions,
            IOptions<MessageMultitenancyOptions> multitenancyOptions
        ) {
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
            _apiOptions = apiOptions?.Value ?? throw new ArgumentNullException(nameof(apiOptions));
            _multitenancyOptions = multitenancyOptions?.Value ?? throw new ArgumentNullException(nameof(multitenancyOptions));
        }

        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName) {
            if (policyName.Equals(MessagesApi.Policies.BeCampaignManager, StringComparison.OrdinalIgnoreCase)) {
                var policy = new AuthorizationPolicyBuilder();
                policy.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                      .RequireAuthenticatedUser()
                      .RequireAssertion(context => context.User.HasScopeClaim(_apiOptions.RequiredScope ?? MessagesApi.Scope))
                      .RequireTenantMembership(_multitenancyOptions.AccessLevel);
                return Task.FromResult(policy.Build());
            }
            return FallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
