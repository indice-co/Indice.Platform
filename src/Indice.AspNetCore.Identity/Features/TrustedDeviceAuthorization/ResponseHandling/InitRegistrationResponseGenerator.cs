using System;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.Features
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
            if (validationResult.InteractionMode == InteractionMode.Fingerprint) {
                return await GenerateFingerprintResponse(validationResult);
            }
            return await Task.FromResult<InitRegistrationResponse>(null);
        }

        private async Task<InitRegistrationResponse> GenerateFingerprintResponse(InitRegistrationRequestValidationResult validationResult) {
            var authorizationCode = new AuthorizationCode {
                ClientId = validationResult.Client.ClientId,
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
