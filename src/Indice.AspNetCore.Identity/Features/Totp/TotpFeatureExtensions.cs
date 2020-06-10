using System;
using System.Security;
using IdentityServer4.Configuration;
using Indice.AspNetCore.Identity.Features;
using Indice.AspNetCore.Identity.Services;
using Indice.Configuration;
using Indice.Services;
using Microsoft.Extensions.Configuration;
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
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/>.</param>
        public static IMvcBuilder AddTotp(this IMvcBuilder builder, Action<Rfc6238AuthenticationServiceOptions> configure = null) => AddTotp<TotpService>(builder, configure);

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <typeparam name="TotpService">The type of <see cref="ITotpService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/>.</param>
        public static IMvcBuilder AddTotp<TotpService>(this IMvcBuilder builder, Action<Rfc6238AuthenticationServiceOptions> configure = null) where TotpService : class, ITotpService {
            builder.Services.TryAddTransient<ITotpService, TotpService>();
            builder.Services.TryAddSingleton(serviceProvider => {
                var options = new Rfc6238AuthenticationServiceOptions {
                    TokenDuration = serviceProvider.GetRequiredService<IConfiguration>().GetSection(Rfc6238AuthenticationServiceOptions.Name).GetValue<int?>(nameof(Rfc6238AuthenticationServiceOptions.TokenDuration))
                };
                configure?.Invoke(options);
                return new Rfc6238AuthenticationService(options);
            });
            builder.ConfigureApplicationPartManager(x => x.FeatureProviders.Add(new TotpFeatureProvider()));
            return builder;
        }

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/>.</param>
        public static IIdentityServerBuilder AddTotp(this IIdentityServerBuilder builder, Action<Rfc6238AuthenticationServiceOptions> configure = null) => AddTotp<TotpService>(builder, configure);

        /// <summary>
        /// Adds all required stuff in order for TOTP strong customer authentication (SCA) to work.
        /// </summary>
        /// <typeparam name="TotpService">The type of <see cref="ITotpService"/> service implementation to use.</typeparam>
        /// <param name="builder">IdentityServer builder Interface.</param>
        /// <param name="configure">Configuration used in <see cref="Rfc6238AuthenticationService"/>.</param>
        public static IIdentityServerBuilder AddTotp<TotpService>(this IIdentityServerBuilder builder, Action<Rfc6238AuthenticationServiceOptions> configure = null) where TotpService : class, ITotpService {
            builder.Services.TryAddTransient<ITotpService, TotpService>();
            builder.Services.TryAddSingleton(serviceProvider => {
                var options = new Rfc6238AuthenticationServiceOptions {
                    TokenDuration = serviceProvider.GetRequiredService<IConfiguration>().GetSection(Rfc6238AuthenticationServiceOptions.Name).GetValue<int?>(nameof(Rfc6238AuthenticationServiceOptions.TokenDuration))
                };
                configure?.Invoke(options);
                return new Rfc6238AuthenticationService(options);
            });
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
