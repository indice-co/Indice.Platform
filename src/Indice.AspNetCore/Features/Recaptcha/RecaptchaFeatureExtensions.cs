using Indice.AspNetCore.Features.Recaptcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddScoped<IRecaptchaService, RecaptchaService>();
        services.AddHttpClient();
        return services;
    }
}
