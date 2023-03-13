using Indice.AspNetCore.Middleware;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions related to configuring the <see cref="ClientIpRestrictionMiddleware"/> on the <seealso cref="IServiceCollection"/></summary>
public static class ClientIpRestrinctionConfiguration
{
    /// <summary>Adds <see cref="ClientIpRestrictionMiddleware"/> related services to the specified <see cref="IServiceCollection"/>.</summary>
    /// <param name="services"></param>
    /// <param name="setupAction"></param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddClientIpRestrinctions(this IServiceCollection services, Action<ClientIpRestrictionOptions> setupAction = null) {
        var existingService = services.Where(x => x.ServiceType == typeof(ClientIpRestrictionOptions)).LastOrDefault();
        if (existingService == null) {
            var options = new ClientIpRestrictionOptions();
            setupAction?.Invoke(options);
            services.AddSingleton((sp) => {
                if (!string.IsNullOrEmpty(options.ConfigurationSectionName)) {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var section = config.GetSection(options.ConfigurationSectionName);
                    if (section != null) {
                        // setup
                        var addresses = section.GetSection("IpAddresses").Get<Dictionary<string, string>>();
                        var rules = section.GetSection("Rules").Get<List<ClientIpRestrictionRule>>();
                        var ignore = section.GetSection("Ignore").Get<List<ClientIpRestrictionIgnore>>();
                        var statusCode = section.GetValue<int?>(nameof(ClientIpRestrictionOptions.HttpStatusCode));
                        var disabled = section.GetValue<bool?>(nameof(ClientIpRestrictionOptions.Disabled));
                        if (addresses is not null) {
                            foreach (var list in addresses) {
                                options.AddIpAddressList(list.Key, list.Value);
                            }
                        }
                        if (rules is not null) {
                            for (var i = 0; i < rules.Count; i++) {
                                var ruleSection = section.GetSection($"Rules:{i}");
                                var ipAddressesList = ruleSection.GetSection("IpAddresses").Get<List<string>>() ?? new List<string>();
                                if (ipAddressesList.Count == 0) {
                                    var value = ruleSection["IpAddresses"];
                                    if (value is not null) { 
                                        ipAddressesList.Add(value);
                                    }
                                }
                                foreach (var ipAddresses in ipAddressesList) {
                                    options.MapPath(rules[i].Path, ipAddresses);
                                }
                            }
                        }
                        if (ignore is not null) {
                            foreach (var item in ignore) {
                                options.IgnorePath(item.Path, item.HttpMethods.Split(','));
                            }
                        }
                        if (statusCode.HasValue) {
                            options.HttpStatusCode = (System.Net.HttpStatusCode)statusCode.Value;
                        }
                        if (disabled.HasValue) {
                            options.Disabled = disabled.Value;
                        }
                    }
                }
                options.ClearUnusedLists();
                return options;
            });
        }
        return services;
    }
}