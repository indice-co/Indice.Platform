using System;
using System.Security;
using IdentityServer4.Configuration;
using Indice.AspNetCore.Identity;
using Indice.AspNetCore.Identity.Features;
using Indice.Configuration;
using Indice.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Adds feature extensions to the <see cref="IMvcBuilder"/>.</summary>
    public static class TotpFeatureExtensions
    {
        /// <summary>Adds all required services and controllers in order for TOTP strong customer authentication (SCA) to work.</summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IMvcBuilder AddTotp(this IMvcBuilder builder, Action<TotpOptions> configure = null) {
            builder.Services.AddTotpServiceFactory(configure);
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new TotpFeatureProvider()));
            return builder;
        }

        /// <summary>Adds all required services and controllers in order for TOTP strong customer authentication (SCA) to work.</summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/> service.</param>
        public static IIdentityServerBuilder AddTotp(this IIdentityServerBuilder builder, Action<TotpOptions> configure = null) {
            builder.Services.AddTotpServiceFactory(configure);
            builder.Services.Configure<IdentityServerOptions>(options => {
                options.Discovery.CustomEntries.Add("totp", new {
                    endpoint = options.IssuerUri.TrimEnd('/') + "/totp",
                    providers = new[] {
                        $"{TotpProviderType.Phone}",
                        $"{TotpProviderType.EToken}"
                    },
                    channels = new[] {
                        $"{TotpDeliveryChannel.Sms}",
                        $"{TotpDeliveryChannel.Viber}",
                        $"{TotpDeliveryChannel.PushNotification}"
                    }
                });
            });
            builder.AddExtensionGrantValidator<TotpGrantValidator>();
            return builder;
        }
    }
}
