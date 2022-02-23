using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions related to configuring the <see cref="ClientIpRestrictionMiddleware"/> on the <seealso cref="IServiceCollection"/>
    /// </summary>
    public static class ClientIpRestrinctionConfiguration
    {
        /// <summary>
        /// Adds <see cref="ClientIpRestrictionMiddleware"/> related services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        /// <returns>The service collection</returns>
        public static IServiceCollection AddClientIpRestrinctions(this IServiceCollection services, Action<ClientIpRestrictionOptions> setupAction = null) {
            var existingService = services.Where(x => x.ServiceType == typeof(ClientIpRestrictionOptions)).LastOrDefault();
            if (existingService == null) {
                var options = new ClientIpRestrictionOptions();
                setupAction?.Invoke(options);
                services.AddSingleton((sp) => {
                    if (!string.IsNullOrEmpty(options.ConfugurationSectionName)) { 
                        var config = sp.GetRequiredService<IConfiguration>();
                        var section = config.GetSection(options.ConfugurationSectionName);
                        if (section != null) {
                            // setup
                            //section.
                        }
                    }
                    return options;
                });
            }
            return services;
        }
    }
}