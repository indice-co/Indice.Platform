using Indice.Features.Messages.AspNetCore.Multitenancy;
using Indice.Features.Messages.AspNetCore.Multitenancy.Authorization;
using Indice.Features.Messages.Core;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Contains extension methods on <see cref="IMvcBuilder"/> for configuring Campaigns API feature.</summary>
    public static class MessageFeatureExtensions
    {
        /// <summary>Adds multitenancy capabilities in the Messages API endpoints.</summary>
        /// <param name="options">Options used to configure the Messages API feature.</param>
        /// <param name="accessLevel">The minimum access level required.</param>
        public static MessageEndpointOptions UseMultiTenancy(this MessageEndpointOptions options, int accessLevel) {
            UseMultiTenancyInternal(options, accessLevel);
            return options;
        }

        /// <summary>Adds multitenancy capabilities in the Messages API endpoints.</summary>
        /// <param name="options">Options used to configure the Messages management API feature.</param>
        /// <param name="accessLevel">The minimum access level required.</param>
        public static MessageManagementOptions UseMultiTenancy(this MessageManagementOptions options, int accessLevel) {
            UseMultiTenancyInternal(options, accessLevel);
            return options;
        }

        private static void UseMultiTenancyInternal(CampaignOptionsBase options, int accessLevel) {
            options.Services.AddSingleton<IAuthorizationPolicyProvider, MultitenantCampaignsPolicyProvider>();
            options.Services.Configure<MessageMultitenancyOptions>(options => options.AccessLevel = accessLevel);
        }
    }
}
