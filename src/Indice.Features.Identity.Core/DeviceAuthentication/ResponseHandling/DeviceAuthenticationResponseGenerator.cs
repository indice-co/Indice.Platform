using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;

internal class DeviceAuthenticationResponseGenerator : IResponseGenerator<DeviceAuthenticationRequestValidationResult, DeviceAuthenticationResponse>
{
    public DeviceAuthenticationResponseGenerator(
        IDeviceAuthenticationCodeChallengeStore codeChallengeStore,
        ISystemClock systemClock
    ) {
        CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
        SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    public IDeviceAuthenticationCodeChallengeStore CodeChallengeStore { get; }
    public ISystemClock SystemClock { get; }

    public async Task<DeviceAuthenticationResponse> Generate(DeviceAuthenticationRequestValidationResult validationResult) {
        var authorizationCode = new DeviceAuthenticationCode {
            ClientId = validationResult.Client.ClientId,
            CodeChallenge = validationResult.CodeChallenge.Sha256(),
            CreationTime = SystemClock.UtcNow.UtcDateTime,
            DeviceId = validationResult.Device.Id.ToString(),
            InteractionMode = validationResult.InteractionMode,
            Lifetime = validationResult.Client.AuthorizationCodeLifetime,
            RequestedScopes = validationResult.RequestedScopes,
            Subject = Principal.Create("TrustedDevice", new Claim(JwtClaimTypes.Subject, validationResult.UserId))
        };
        var challenge = await CodeChallengeStore.GenerateChallenge(authorizationCode);
        return new DeviceAuthenticationResponse {
            Challenge = challenge
        };
    }
}
