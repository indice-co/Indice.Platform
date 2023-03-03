using System.Security.Claims;
using Indice.Security;

namespace IdentityServer4.Models;

internal static class ClientExtensions
{
    public static bool IsMobile(this Client client) => client.Claims.Contains(new ClientClaim(BasicClaimTypes.MobileClient, bool.TrueString.ToLower(), ClaimValueTypes.Boolean));
}
