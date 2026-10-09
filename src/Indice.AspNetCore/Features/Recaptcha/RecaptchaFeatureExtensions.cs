using Indice.AspNetCore.Features.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds feature extensions to the <see cref="IMvcBuilder"/>.</summary>
public static class RecaptchaFeatureExtensions {

    /// <summary>
    /// Adds reCAPTCHA services to the application.
    /// </summary>
    /// <param name="services">The application services.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureAction">An optional action to configure the reCAPTCHA options.</param>
    public static IServiceCollection AddRecaptcha(this IServiceCollection services, IConfiguration configuration, Action<RecaptchaOptions>? configureAction = null) {
        services.Configure<RecaptchaOptions>(configuration.GetSection(RecaptchaOptions.SectionName));
        if (configureAction is not null) {
            services.Configure(configureAction);
        }
        services.AddScoped<IRecaptchaService>(serviceProvider => {
            var options = serviceProvider.GetRequiredService<IOptions<RecaptchaOptions>>().Value;
            return options.Provider switch {
                CaptchaProviderType.HCaptcha => serviceProvider.GetRequiredService<HCaptchaService>(),
                CaptchaProviderType.Recaptcha => serviceProvider.GetRequiredService<RecaptchaService>(),
                _ => serviceProvider.GetRequiredService<NoOpRecaptchaService>()
            };
        });
        services.AddHttpClient();
        return services;
    }
}
