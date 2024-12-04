using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using IdentityServer4.Models;
using System.Globalization;
using IdentityServer4.Internal.Extensions;
using IdentityServer4.Extensions;

namespace Indice.Features.Identity.Core.TokenCreation;

/// <inheritdoc />
public class ExtendedTokenCreationService : ITokenCreationService
{
    private readonly IKeyMaterialService _keys;
    private readonly IdentityServerOptions _options;
    private readonly ILogger<DefaultTokenCreationService> _logger;

    /// <summary>Gets the current time, primarily for unit testing.</summary>
    protected TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    /// <summary>
    /// Creates a new instance for <see cref="ExtendedTokenCreationService"/>.
    /// </summary>
    /// <param name="keys">The key service.</param>
    /// <param name="options">The options/</param>
    /// <param name="logger">The logger.</param>
    public ExtendedTokenCreationService(
        IKeyMaterialService keys,
        IdentityServerOptions options,
        ILogger<DefaultTokenCreationService> logger) {
        _keys = keys;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> CreateTokenAsync(Token token) {
        var header = await CreateHeaderAsync(token);
        var payload = await CreatePayloadAsync(token);

        return await CreateJwtAsync(new JwtSecurityToken(header, payload));
    }

    /// <summary>
    /// Creates the JWT header
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The JWT header</returns>
    protected async Task<JwtHeader> CreateHeaderAsync(Token token) {
        var credential = await _keys.GetSigningCredentialsAsync(token.AllowedSigningAlgorithms);

        if (credential == null) {
            throw new InvalidOperationException("No signing credential is configured. Can't create JWT token");
        }

        var header = new JwtHeader(credential);

        // emit x5t claim for backwards compatibility with v4 of MS JWT library
        if (credential.Key is X509SecurityKey x509Key) {
            var cert = x509Key.Certificate;
            if (TimeProvider.GetUtcNow().UtcDateTime > cert.NotAfter) {
                _logger.LogWarning("Certificate {subjectName} has expired on {expiration}", cert.Subject, cert.NotAfter.ToString(CultureInfo.InvariantCulture));
            }

            header["x5t"] = Base64Url.Encode(cert.GetCertHash());
        }

        if (token.Type == IdentityServerConstants.TokenTypes.AccessToken) {
            if (_options.AccessTokenJwtType.IsPresent()) {
                header["typ"] = _options.AccessTokenJwtType;
            }
        }

        return header;
    }

    /// <summary>
    /// Creates the JWT payload
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The JWT payload</returns>
    protected async Task<JwtPayload> CreatePayloadAsync(Token token) {
        await Task.CompletedTask;
        
        var payload = new JwtPayload(
                token.Issuer,
                null,
                null,
                TimeProvider.GetUtcNow().UtcDateTime,
                TimeProvider.GetUtcNow().UtcDateTime.AddSeconds(token.Lifetime));

        foreach (var aud in token.Audiences) {
            payload.AddClaim(new Claim(JwtClaimTypes.Audience, aud));
        }

        var amrClaims = token.Claims.Where(x => x.Type == JwtClaimTypes.AuthenticationMethod).ToArray();
        var scopeClaims = token.Claims.Where(x => x.Type == JwtClaimTypes.Scope).ToArray();
        var jsonClaims = token.Claims.Where(x => x.ValueType == IdentityServerConstants.ClaimValueTypes.Json).ToList();

        // add confirmation claim if present (it's JSON valued)
        if (token.Confirmation.IsPresent()) {
            jsonClaims.Add(new Claim(JwtClaimTypes.Confirmation, token.Confirmation, IdentityServerConstants.ClaimValueTypes.Json));
        }

        var normalClaims = token.Claims
            .Except(amrClaims)
            .Except(jsonClaims)
            .Except(scopeClaims);

        payload.AddClaims(normalClaims);

        // scope claims
        if (!IEnumerableExtensions.IsNullOrEmpty(scopeClaims)) {
            var scopeValues = scopeClaims.Select(x => x.Value).ToArray();

            if (_options.EmitScopesAsSpaceDelimitedStringInJwt) {
                payload.Add(JwtClaimTypes.Scope, string.Join(" ", scopeValues));
            } else {
                payload.Add(JwtClaimTypes.Scope, scopeValues);
            }
        }

        // amr claims
        if (!IEnumerableExtensions.IsNullOrEmpty(amrClaims)) {
            var amrValues = amrClaims.Select(x => x.Value).Distinct().ToArray();
            payload.Add(JwtClaimTypes.AuthenticationMethod, amrValues);
        }

        // deal with json types
        // calling ToArray() to trigger JSON parsing once and so later 
        // collection identity comparisons work for the anonymous type
        try {
            var jsonTokens = jsonClaims.Select(x => new { x.Type, JsonValue = JsonDocument.Parse(x.Value).RootElement }).ToArray();
            // var jsonTokens2 = jsonClaims.Select(x => new { x.Type, JsonValue = JsonSerializer.SerializeToElement(x.Value) }).ToArray();


            var jsonObjects = jsonTokens.Where(x => x.JsonValue.ValueKind == JsonValueKind.Object).ToArray();
            var jsonObjectGroups = jsonObjects.GroupBy(x => x.Type).ToArray();
            foreach (var group in jsonObjectGroups) {
                if (payload.ContainsKey(group.Key)) {
                    throw new Exception($"Can't add two claims where one is a JSON object and the other is not a JSON object ({group.Key})");
                }

                if (group.Skip(1).Any()) {
                    // add as array
                    payload.Add(group.Key, group.Select(x => x.JsonValue).ToArray());
                } else {
                    // add just one
                    payload.Add(group.Key, group.First().JsonValue);
                }
            }

            var jsonArrays = jsonTokens.Where(x => x.JsonValue.ValueKind == JsonValueKind.Array).ToArray();
            var jsonArrayGroups = jsonArrays.GroupBy(x => x.Type).ToArray();

            foreach (var group in jsonArrayGroups) {
                if (payload.ContainsKey(group.Key)) {
                    throw new Exception(
                        $"Can't add two claims where one is a JSON array and the other is not a JSON array ({group.Key})");
                }

                payload.Add(group.Key, group.SelectMany(x => x.JsonValue.EnumerateArray()).ToArray());
            }

            var unsupportedJsonTokens = jsonTokens.Except(jsonObjects).Except(jsonArrays).ToArray();
            var unsupportedJsonClaimTypes = unsupportedJsonTokens.Select(x => x.Type).Distinct().ToArray();
            if (unsupportedJsonClaimTypes.Any()) {
                throw new Exception(
                    $"Unsupported JSON type for claim types: {unsupportedJsonClaimTypes.Aggregate((x, y) => x + ", " + y)}");
            }

            return payload;
        } catch (Exception ex) {
            _logger.LogCritical(ex, "Error creating a JSON valued claim");
            throw;
        }
    }

    /// <summary>
    /// Applies the signature to the JWT
    /// </summary>
    /// <param name="jwt">The JWT object.</param>
    /// <returns>The signed JWT</returns>
    protected virtual Task<string> CreateJwtAsync(JwtSecurityToken jwt) {
        var handler = new JwtSecurityTokenHandler();
        return Task.FromResult(handler.WriteToken(jwt));
    }
}