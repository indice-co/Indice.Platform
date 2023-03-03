using IdentityServer4.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Models;
using Indice.Features.Identity.Core.DeviceAuthentication.Stores;
using Indice.Features.Identity.Core.DeviceAuthentication.Validation;
using Microsoft.AspNetCore.Authentication;

namespace Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;

internal class InitRegistrationResponseGenerator : IResponseGenerator<InitRegistrationRequestValidationResult, InitRegistrationResponse>
{
    public InitRegistrationResponseGenerator(
        IDeviceAuthenticationCodeChallengeStore codeChallengeStore,
        ISystemClock systemClock
    ) {
        CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
        SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
    }

    public IDeviceAuthenticationCodeChallengeStore CodeChallengeStore { get; }
    public ISystemClock SystemClock { get; }

    public async Task<InitRegistrationResponse> Generate(InitRegistrationRequestValidationResult validationResult) {
        var authorizationCode = new DeviceAuthenticationCode {
            ClientId = validationResult.Client.ClientId,
            DeviceId = validationResult.DeviceId,
            InteractionMode = validationResult.InteractionMode,
            CodeChallenge = validationResult.CodeChallenge.Sha256(),
            CreationTime = SystemClock.UtcNow.UtcDateTime,
            Lifetime = validationResult.Client.AuthorizationCodeLifetime,
            RequestedScopes = validationResult.RequestedScopes,
            Subject = validationResult.Principal
        };
        var challenge = await CodeChallengeStore.GenerateChallenge(authorizationCode);
        return new InitRegistrationResponse { 
            Challenge = challenge
        };
    }
}
