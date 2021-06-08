using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Indice.AspNetCore.Authentication.Apple
{
    /// <summary>
    /// This class will denerate tokens for a valid apple privateKey
    /// </summary>
    public class AppleTokenGenerator
    {
        /// <summary>
        /// Create a new client secret (Per request)
        /// </summary>
        /// <param name="issuer">your accounts team ID found in the dev portal</param>
        /// <param name="audience">Apple authority.</param>
        /// <param name="subject">The service id (client_id)</param>
        /// <param name="privateKey">Base64 encoded private key</param>
        /// <param name="privateKeyId">The key id</param>
        /// <param name="duration">expiry can be a maximum of 6 months - generate one per request or re-use until expiration. Defaults to 5 minutes</param>
        /// <returns></returns>
        public static string CreateNewToken(string issuer, string audience, string subject, string privateKey, string privateKeyId, TimeSpan? duration = null) {
            duration = duration ?? TimeSpan.FromMinutes(5);
            var now = DateTime.UtcNow;
            var ecdsa = ECDsa.Create();
            if (privateKey.StartsWith('-')) {
#if NETCOREAPP3_1
                var lines = privateKey.Split('\n');
                privateKey = string.Join("", privateKey.Skip(1).Take(lines.Length - 2));
                ecdsa?.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
#else
                ecdsa?.ImportFromPem(privateKey);
#endif
            } else {
                ecdsa?.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKey), out _);
            }
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
}
