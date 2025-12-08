using Azure.Core.Serialization;
using Indice.AspNetCore.Features.SignalrEnpoint.Interfaces;
using Indice.Serialization;
using Indice.SignalR.Endpoints;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Indice.AspNetCore.Features.SignalrEnpoint;

/// <summary>
/// 
/// </summary>
public static class SignalrEndpointExtensions
{
    /// <summary>
    /// Adds SignalR endpoint services to the application.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureAction"></param>
    public static IHostApplicationBuilder AddSignalREndpoints(this IHostApplicationBuilder builder, Action<SignalREndpointsOptions>? configureAction = null) {
        // Use SignalR SDK to create a new HubContext and a negotiation response
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(serviceManagerOptions => {
                serviceManagerOptions.ConnectionString = builder.Configuration["AzureSignalRConnectionString"];
                serviceManagerOptions.ServiceTransportType = ServiceTransportType.Transient;
                serviceManagerOptions.UseJsonObjectSerializer(
                    new JsonObjectSerializer(JsonSerializerOptionDefaults.GetDefaultSettings()));
            })
            .BuildServiceManager();
        builder.Services.AddSingleton(serviceManager);


        builder.Services.Configure<SignalREndpointsOptions>(builder.Configuration.GetSection(nameof(SignalREndpointsOptions)));
        if(configureAction is not null) {
            builder.Services.PostConfigure(configureAction);
        }
        builder.Services.TryAddSingleton<HubContextStore>();
        builder.Services.TryAddTransient<ISignalRNegotiateService, SignalRNegotiateService>();
        builder.Services.TryAddTransient<ISignalRBroadcastService, SignalRBroadcastService>();
        return builder;
    }

}
