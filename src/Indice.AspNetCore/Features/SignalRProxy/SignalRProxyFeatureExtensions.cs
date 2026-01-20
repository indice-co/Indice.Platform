using Indice.AspNetCore.Features.SignalRProxy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
    /// <param name="builder"></param>
    /// <param name="configureAction"></param>
    public static IHostApplicationBuilder AddSignalRProxy(this IHostApplicationBuilder builder, Action<SignalRProxyOptions>? configureAction = null) {
        builder.Services.AddSignalRProxyCoreServices(options => {
            options.ConnectionString = builder.Configuration.GetConnectionString("SignalR");
        });
        
        // Register default user ID resolver
        builder.Services.AddSingleton<ISignalRProxyUserIdResolver, DefaultSignalRProxyUserIdResolver>();
        
        builder.Services.Configure<SignalRProxyOptions>(builder.Configuration.GetSection(SignalRProxyOptions.SectionName));
        builder.Services.Configure<SignalRProxyOptions>(options => {
            options.Services = builder.Services;
        });
        
        if(configureAction is not null) {
            builder.Services.PostConfigure(configureAction);
        }
        return builder;
    }
}
