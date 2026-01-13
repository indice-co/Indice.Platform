using Azure.Core.Serialization;
using Indice.Serialization;
using Indice.Services;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Features.SignalRProxy;

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
        builder.Services.Configure<SignalRProxyOptions>(builder.Configuration.GetSection(SignalRProxyOptions.SectionName));
        if(configureAction is not null) {
            builder.Services.PostConfigure(configureAction);
        }
        return builder;
    }

}
