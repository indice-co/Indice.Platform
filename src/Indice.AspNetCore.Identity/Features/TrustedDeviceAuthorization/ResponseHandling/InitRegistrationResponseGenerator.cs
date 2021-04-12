using System;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal class InitRegistrationResponseGenerator
    {
        private readonly IAuthorizationCodeChallengeStore _codeChallengeStore;

        public InitRegistrationResponseGenerator(IAuthorizationCodeChallengeStore authorizationCodeChallengeStore) {
            _codeChallengeStore = authorizationCodeChallengeStore ?? throw new ArgumentNullException(nameof(authorizationCodeChallengeStore));
        }

        public async Task<InitRegistrationResponse> Generate(InitRegistrationRequestValidationResult validationResult) {
            if (validationResult.InteractionMode == InteractionMode.Fingerprint) {
                return await GenerateFingerprintResponse(validationResult);
            }
            return await Task.FromResult<InitRegistrationResponse>(null);
        }

        private async Task<InitRegistrationResponse> GenerateFingerprintResponse(InitRegistrationRequestValidationResult validationResult) {
            var authorizationCode = new AuthorizationCode {
                ClientId = validationResult.Client.ClientId,
                CodeChallenge = validationResult.CodeChallenge,
                CodeChallengeMethod = validationResult.CodeChallengeMethod,
                CreationTime = DateTime.UtcNow.Date,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes,
                Subject = validationResult.Principal
            };
            var id = await _codeChallengeStore.Create(authorizationCode);
            return new InitRegistrationResponse {
                Challenge = id
            };
        }
    }
}
