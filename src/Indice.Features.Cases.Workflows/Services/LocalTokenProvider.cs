using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Indice.Features.Cases.Workflows.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.Cases.Workflows.Services;

public class LocalTokenProvider(IConfiguration configuration, IDistributedCache cache)
{
    private const string LocalTokenCacheKey = "workflows:localBearerToken";

    public string GetBearerToken() {
        var tokenString = cache.GetString(LocalTokenCacheKey);
        if (!string.IsNullOrWhiteSpace(tokenString)) {
            return tokenString;
        }
        
        var key = Encoding.UTF8.GetBytes("SuperSecretKeyThatIsExactly32BytesLong123!");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var claims = CasesClaimsPrincipalExtensions.Claims;
        // var header = new JwtHeader(credentials);
        
        var header = new JwtHeader(credentials) {
            { "kid", "your-key-id" }
        };
        
        var payload = new JwtPayload(
            issuer: configuration.GetHost(),
            audience: "cases",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(30)
        );

        var token = new JwtSecurityToken(header, payload);
        tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        cache.SetString(LocalTokenCacheKey, tokenString, new DistributedCacheEntryOptions {
            AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(20)
        });

        return tokenString;
    }
}