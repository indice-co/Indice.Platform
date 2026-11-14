using System.Security.Claims;
using Indice.Security;
namespace Duende.IdentityServer.Models;

internal static class ClientExtensions
{
    public static bool IsMobile(this Client client) => client.Claims.Contains(new ClientClaim(BasicClaimTypes.MobileClient, bool.TrueString.ToLower(), ClaimValueTypes.Boolean));
}
