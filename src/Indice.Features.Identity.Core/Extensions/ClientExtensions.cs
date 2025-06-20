using System.Security.Claims;
using Indice.Security;
#if NET9_0_OR_GREATER
namespace Duende.IdentityServer.Models;
#else
namespace IdentityServer4.Models;
#endif

internal static class ClientExtensions
{
    public static bool IsMobile(this Client client) => client.Claims.Contains(new ClientClaim(BasicClaimTypes.MobileClient, bool.TrueString.ToLower(), ClaimValueTypes.Boolean));
}
