using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary></summary>
public static class IdentityBuilderUIExtensions
{
    /// <summary></summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static IServiceCollection AddIdentityUI(this IServiceCollection services, IConfiguration configuration) {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddGeneralSettings(configuration);
        services.AddClientAwareViewLocationExpander();
        services.AddMarkdown();
        return services;
    }
}
