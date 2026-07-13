using System.Security.Claims;
using Duende.IdentityModel;

namespace Indice.Features.Identity.Core.MobileSessions;

/// <summary>Creates and finds session ids for token endpoint (mobile) flows.</summary>
internal static class MobileSessionIds
{
    /// <summary>Creates a new session id.</summary>
    public static string Create() => CryptoRandom.CreateUniqueId(16, CryptoRandom.OutputFormat.Hex);

    /// <summary>Finds the session id claim in the given claims.</summary>
    public static string? Find(IEnumerable<Claim>? claims) => claims?.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId)?.Value;
}