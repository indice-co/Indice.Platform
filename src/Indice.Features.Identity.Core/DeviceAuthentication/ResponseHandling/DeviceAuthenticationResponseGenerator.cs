using System.Security.Claims;
using IdentityModel;
#if NET9_0_OR_GREATER
using Duende.IdentityServer.Models;
#else
using IdentityServer4.Models;
#endif
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;

namespace Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;

internal class DeviceAuthenticationResponseGenerator : IResponseGenerator<DeviceAuthenticationRequestValidationResult, DeviceAuthenticationResponse>
{
    public DeviceAuthenticationResponseGenerator(
        IDeviceAuthenticationCodeChallengeStore codeChallengeStore
    ) {
        CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
    }

    /// <summary>
    /// Gets the current time, primarily for unit testing.
    /// </summary>
    protected TimeProvider TimeProvider { get; private set; } = TimeProvider.System;

    public IDeviceAuthenticationCodeChallengeStore CodeChallengeStore { get; }

    public async Task<DeviceAuthenticationResponse> Generate(DeviceAuthenticationRequestValidationResult validationResult) {
        var authorizationCode = new DeviceAuthenticationCode {
            ClientId = validationResult.Client?.ClientId,
            CodeChallenge = validationResult.CodeChallenge.Sha256(),
            CreationTime = TimeProvider.GetUtcNow().UtcDateTime,
            DeviceId = validationResult.Device?.Id.ToString(),
            InteractionMode = validationResult.InteractionMode,
            Lifetime = validationResult.Client?.AuthorizationCodeLifetime ?? 300,
            RequestedScopes = validationResult.RequestedScopes,
            Subject = Principal.Create("TrustedDevice", new Claim(JwtClaimTypes.Subject, validationResult.UserId!))
        };
        var challenge = await CodeChallengeStore.GenerateChallenge(authorizationCode);
        return new DeviceAuthenticationResponse {
            Challenge = challenge
        };
    }
}
