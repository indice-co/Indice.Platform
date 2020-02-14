using IdentityServer4.Configuration;
using Indice.AspNetCore.Features;
using Indice.AspNetCore.Identity.Services;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds feature extensions to the <see cref="IMvcBuilder"/>.
    /// </summary>
    public static class TotpFeatureExtensions
    {
        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        public static IMvcBuilder AddTotp(this IMvcBuilder builder) => AddTotp<TotpService>(builder);

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <typeparam name="TotpService">The type of <see cref="ITotpService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        public static IMvcBuilder AddTotp<TotpService>(this IMvcBuilder builder) where TotpService : class, ITotpService {
            builder.Services.TryAddTransient<ITotpService, TotpService>();
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new TotpFeatureProvider()));
            return builder;
        }

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        public static IIdentityServerBuilder AddTotp(this IIdentityServerBuilder builder) => AddTotp<TotpService>(builder);

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <typeparam name="TotpService">The type of <see cref="ITotpService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        public static IIdentityServerBuilder AddTotp<TotpService>(this IIdentityServerBuilder builder) where TotpService : class, ITotpService {
            builder.Services.TryAddTransient<ITotpService, TotpService>();
            builder.Services.Configure<IdentityServerOptions>((options) => {
                options.Discovery.CustomEntries.Add("totp", new {
                    endpoint = options.IssuerUri.TrimEnd('/') + "/totp",
                    providers = new[] { $"{TotpProviderType.Phone}", $"{TotpProviderType.EToken}" },
                    channels = new[] { $"{TotpDeliveryChannel.Sms}" }
                });
            });
            builder.AddExtensionGrantValidator<TotpGrantValidator>();
            return builder;
        }
    }
}
