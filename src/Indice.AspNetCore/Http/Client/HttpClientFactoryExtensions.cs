using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extensions for <see cref="IHttpClientFactory"/>.</summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Retrieves the loopback URI for the server, replacing the host with "localhost" while finding out the internal port.
    /// This makes it easier to make requests that talk to the server self without going through the external network stack.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> instance used to resolve the server and its features.</param>
    /// <returns>A <see cref="Uri"/> representing the server's loopback address with the host set to "localhost" and the internal port.</returns>
    public static Uri? GetServerLoopbackUri(this IServiceProvider serviceProvider) {
        var server = serviceProvider.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()!.Addresses.FirstOrDefault() ??
                                     serviceProvider.GetRequiredService<IConfiguration>()["ASPNETCORE_URLS"]?.Split(",", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (address == null) {
            return null;
        }

        var uriBuilder = new UriBuilder(address) {
            Host = "localhost"
        };
        return uriBuilder.Uri;
    }
}
