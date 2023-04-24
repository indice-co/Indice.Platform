using Indice.Features.Identity.UI;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods on <see cref="IServiceCollection"/> for registering required services for Identity UI pages feature.</summary>
public static class IdentityBuilderUIExtensions
{
    /// <summary>Adds the required services and pages for Identity UI pages features.</summary>
    /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public static IServiceCollection AddIdentityUI(this IServiceCollection services, IConfiguration configuration) {
        services.ConfigureOptions(typeof(IdentityUIConfigureOptions));
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddGeneralSettings(configuration);
        services.AddClientAwareViewLocationExpander();
        services.AddMarkdown();
        return services;
    }
}
