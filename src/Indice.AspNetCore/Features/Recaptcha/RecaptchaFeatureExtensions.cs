using Indice.AspNetCore.Features.Recaptcha;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds feature extensions to the <see cref="IMvcBuilder"/>.</summary>
public static class RecaptchaFeatureExtensions {

    /// <summary>
    /// Adds SignalR endpoint services to the application.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configureAction">An optional action to configure the SignalR proxy options.</param>
    public static IHostApplicationBuilder AddRecaptcha(this IHostApplicationBuilder builder, Action<RecaptchaOptions>? configureAction = null) {
        builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection(RecaptchaOptions.SectionName));
        
        if (configureAction is not null) {
            builder.Services.Configure(configureAction);
        }

        builder.Services.TryAddScoped<IRecaptchaService, RecaptchaService>();
        builder.Services.AddHttpClient();
        return builder;
    }
}
