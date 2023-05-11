using Indice.Features.Identity.Core;

namespace Indice.Features.Identity.UI;

/// <summary>Common authorization policy names.</summary>
public class PolicyNames
{
    /// <summary>User must be authenticated with <see cref="CustomGrantTypes.DeviceAuthentication"/> grant type.</summary>
    public const string BeDeviceAuthenticated = "BeDeviceAuthenticated";
}
