using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Features;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains methods for configuring the trusted device authorization feature.
    /// </summary>
    public static class TrustedDeviceAuthorizationConfiguration
    {
        /// <summary>
        /// Register the endpoints and required services for trusted device authorization.
        /// </summary>
        /// <param name="builder">IdentityServer builder interface.</param>
        public static IIdentityServerBuilder AddTrustedDeviceAuthorization(this IIdentityServerBuilder builder) {
            builder.AddEndpoint<TrustedDeviceRegistrationEndpoint>("TrustedDeviceAuthorization", "/my/devices/register/init");
            builder.Services.AddTransient<ITrustedDeviceAuthorizationCodeChallengeStore, DefaultTrustedDeviceAuthorizationCodeChallengeStore>();
            builder.Services.AddTransient<BearerTokenUsageValidator>();
            builder.Services.AddTransient<ITrustedDeviceRegistrationRequestValidator, TrustedDeviceRegistrationRequestValidator>();
            builder.Services.AddTransient<ITrustedDeviceRegistrationResponseGenerator, TrustedDeviceRegistrationResponseGenerator>();
            return builder;
        }
    }
}
