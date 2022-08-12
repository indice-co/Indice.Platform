using System.Security.Claims;
using Indice.Features.Messages.Core;
using Indice.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Messages.Worker.Azure
{
    /// <summary>Extensions on <see cref="MessageEndpointOptions"/>.</summary>
    public static class MessageApiOptionsExtensions
    {
        /// <summary>Adds <see cref="IEventDispatcher"/> using Azure Storage as a queuing mechanism.</summary>
        /// <param name="options">Options used to configure the Campaigns API feature.</param>
        /// <param name="configure">Configure the available options. Null to use defaults.</param>
        public static void UseEventDispatcherAzure(this MessageEndpointOptions options, Action<IServiceProvider, MessagesEventDispatcherAzureOptions> configure = null) {
            options.Services.AddEventDispatcherAzure(KeyedServiceNames.EventDispatcherServiceKey, (serviceProvider, options) => {
                var eventDispatcherOptions = new MessagesEventDispatcherAzureOptions {
                    ConnectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(EventDispatcherAzure.CONNECTION_STRING_NAME),
                    Enabled = true,
                    EnvironmentName = serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName,
                    ClaimsPrincipalSelector = ClaimsPrincipal.ClaimsPrincipalSelector ?? (() => ClaimsPrincipal.Current)
                };
                configure?.Invoke(serviceProvider, eventDispatcherOptions);
                options.ClaimsPrincipalSelector = eventDispatcherOptions.ClaimsPrincipalSelector;
                options.ConnectionString = eventDispatcherOptions.ConnectionString;
                options.Enabled = eventDispatcherOptions.Enabled;
                options.EnvironmentName = eventDispatcherOptions.EnvironmentName;
                options.QueueMessageEncoding = eventDispatcherOptions.QueueMessageEncoding;
                options.TenantIdSelector = eventDispatcherOptions.TenantIdSelector;
                options.UseCompression = true;
            });
        }
    }
}
