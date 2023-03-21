using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Indice.AspNetCore.Authentication.Apple;

/// <summary>This class will generate tokens for a valid apple private key.</summary>
public class AppleTokenGenerator
{
    /// <summary>Create a new client secret (per request).</summary>
    /// <param name="issuer">Your account's team ID found in the dev portal.</param>
    /// <param name="audience">Apple authority.</param>
    /// <param name="subject">The service id (client_id).</param>
    /// <param name="privateKey">Base64 encoded private key.</param>
    /// <param name="privateKeyId">The key id.</param>
    /// <param name="duration">Expiry can be a maximum of 6 months - generate one per request or re-use until expiration. Defaults to 5 minutes.</param>
    public static string CreateNewToken(string issuer, string audience, string subject, string privateKey, string privateKeyId, TimeSpan? duration = null) {
        duration ??= TimeSpan.FromMinutes(5);
        var now = DateTime.UtcNow;
        var ecdsa = ECDsa.Create();
        if (privateKey.StartsWith('-')) {
            var lines = privateKey.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            privateKey = string.Join("", lines.Skip(1).Take(lines.Length - 2));

        }
//#if NETCOREAPP3_1
        ecdsa?.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
//#else
        //ecdsa?.ImportFromPem(privateKey);
//#endif
        var handler = new JsonWebTokenHandler();
        var key = new ECDsaSecurityKey(ecdsa) {
            KeyId = privateKeyId
        };
        return handler.CreateToken(new SecurityTokenDescriptor {
            Issuer = issuer,
            Audience = audience,
            Claims = new Dictionary<string, object> { { "sub", subject } },
            Expires = now.Add(duration.Value),
            IssuedAt = now,
            NotBefore = now,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.EcdsaSha256)
        });
    }
}
