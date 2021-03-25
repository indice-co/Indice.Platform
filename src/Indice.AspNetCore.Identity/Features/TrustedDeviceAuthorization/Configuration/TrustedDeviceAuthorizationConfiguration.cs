using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Features;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class TrustedDeviceAuthorizationConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        public static IIdentityServerBuilder AddTrustedDeviceAuthorization(this IIdentityServerBuilder builder) {
            builder.AddEndpoint<TrustedDeviceRegistrationEndpoint>("TrustedDeviceAuthorization", "/my/devices");
            builder.Services.AddTransient<ITrustedDeviceAuthorizationCodeChallengeStore, DefaultTrustedDeviceAuthorizationCodeChallengeStore>();
            builder.Services.AddTransient<BearerTokenUsageValidator>();
            builder.Services.AddTransient<ITrustedDeviceRegistrationRequestValidator, TrustedDeviceRegistrationRequestValidator>();
            builder.Services.AddTransient<ITrustedDeviceRegistrationResponseGenerator, TrustedDeviceRegistrationResponseGenerator>();
            return builder;
        }
    }
}
