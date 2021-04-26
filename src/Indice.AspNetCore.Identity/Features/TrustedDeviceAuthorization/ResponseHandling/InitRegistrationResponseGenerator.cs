using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class InitRegistrationResponseGenerator : IResponseGenerator<InitRegistrationRequestValidationResult, InitRegistrationResponse>
    {
        public InitRegistrationResponseGenerator(
            IAuthorizationCodeChallengeStore authorizationCodeChallengeStore,
            ISystemClock systemClock
        ) {
            CodeChallengeStore = authorizationCodeChallengeStore ?? throw new ArgumentNullException(nameof(authorizationCodeChallengeStore));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public IAuthorizationCodeChallengeStore CodeChallengeStore { get; }
        public ISystemClock SystemClock { get; }

        public async Task<InitRegistrationResponse> Generate(InitRegistrationRequestValidationResult validationResult) {
            var authorizationCode = new TrustedDeviceAuthorizationCode {
                ClientId = validationResult.Client.ClientId,
                DeviceId = validationResult.DeviceId,
                InteractionMode = validationResult.InteractionMode,
                CodeChallenge = validationResult.CodeChallenge.Sha256(),
                CodeChallengeMethod = validationResult.CodeChallengeMethod,
                CreationTime = SystemClock.UtcNow.UtcDateTime,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes,
                Subject = validationResult.Principal
            };
            // Challenge is created but also stored in persisted grants.
            var challenge = await CodeChallengeStore.GenerateChallenge(authorizationCode);
            return new InitRegistrationResponse { Challenge = challenge };
        }
    }
}
