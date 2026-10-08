using Indice.AspNetCore.Features.SignalRProxy;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;


using Microsoft.Extensions.Hosting;

/// <summary>
/// Service registration extensions for SignalR endpoints.
/// </summary>
public static class SignalRProxyFeatureExtensions
{
    /// <summary>
    /// Adds SignalR endpoint services to the application.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="configureAction">An optional action to configure the SignalR proxy options.</param>
    public static IHostApplicationBuilder AddSignalRProxy(this IHostApplicationBuilder builder, Action<SignalRProxyOptions>? configureAction = null) {
        builder.Services.AddSignalRProxyCoreServices();
        // Register default user ID resolver
        builder.Services.AddSingleton<ISignalRProxyUserIdResolver, DefaultSignalRProxyUserIdResolver>();
        
        // Register default group name validator (no-op)
        builder.Services.AddSingleton<ISignalRProxyGroupNameValidator, NoOpGroupNameValidator>();
        
        builder.Services.Configure<SignalRProxyOptions>(builder.Configuration.GetSection(SignalRProxyOptions.SectionName));
        builder.Services.Configure<SignalRProxyOptions>(options => {
            options.Services = builder.Services;
        });
        
        if(configureAction is not null) {
            builder.Services.Configure(configureAction);
        }
        builder.Services.ConfigureOptions<PostConfigureSignalRProxyCoreOptions>();

        return builder;
    }
}
