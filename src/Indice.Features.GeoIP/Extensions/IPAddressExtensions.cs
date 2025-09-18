using System.Net;
using System.Net.Sockets;

namespace Indice.Features.GeoIP.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IPAddress"/>.
/// </summary>
public static class IPAddressExtensions
{
    /// <summary>
    /// Determines whether the specified IP address is a private address.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns></returns>
    public static bool IsPrivate(this IPAddress ipAddress) {
        if (ipAddress.AddressFamily is AddressFamily.InterNetwork) {
            var bytes = ipAddress.GetAddressBytes();
            return
                bytes[0] == 10 ||                                     // 10.0.0.0/8
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.0.0/12
                (bytes[0] == 192 && bytes[1] == 168);                 // 192.168.0.0/16
        }

        if (ipAddress.AddressFamily is AddressFamily.InterNetworkV6) {
            return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal;
        }

        return false;
    }
}
