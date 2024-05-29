using IdentityModel;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Identity.Core.TokenCreation;
/// <summary>
/// Logic for creating security tokens extended with <see cref="System.Text.Json"/> for <see cref="IdentityServerConstants.ClaimValueTypes.Json"/> claim types.
/// </summary>
public class ExtendedTokenCreationService : DefaultTokenCreationService
{
    private readonly ISystemClock _clock;
    private readonly IdentityServerOptions _options;
    private readonly ILogger<DefaultTokenCreationService> _logger;

    /// <summary>
    /// Creates a new instance for <see cref="ExtendedTokenCreationService"/>.
    /// Creates a new instance for <see cref="ExtendedTokenCreationService"/>.
    /// </summary>
    /// <param name="clock">The clock.</param>
    /// <param name="keys">The key service.</param>
    /// <param name="options">The options/</param>
    /// <param name="logger">The logger.</param>
    public ExtendedTokenCreationService(
        ISystemClock clock,
        IKeyMaterialService keys,
        IdentityServerOptions options,
        ILogger<DefaultTokenCreationService> logger)
        : base(clock, keys, options, logger) {
        _clock = clock;
        _options = options;
        _logger = logger;
    }
    
    /// <inheritdoc />
    protected override async Task<JwtPayload> CreatePayloadAsync(Token token) {
        await Task.CompletedTask;

        var payload = new JwtPayload(
                token.Issuer,
                null,
                null,
                _clock.UtcNow.UtcDateTime,
                _clock.UtcNow.UtcDateTime.AddSeconds(token.Lifetime));

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
        if (!scopeClaims.IsNullOrEmpty()) {
            var scopeValues = scopeClaims.Select(x => x.Value).ToArray();

            if (_options.EmitScopesAsSpaceDelimitedStringInJwt) {
                payload.Add(JwtClaimTypes.Scope, string.Join(" ", scopeValues));
            } else {
                payload.Add(JwtClaimTypes.Scope, scopeValues);
            }
        }

        // amr claims
        if (!amrClaims.IsNullOrEmpty()) {
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
}