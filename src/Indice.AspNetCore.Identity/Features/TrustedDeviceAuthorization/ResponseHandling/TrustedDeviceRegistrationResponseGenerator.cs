using System;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationResponseGenerator : ITrustedDeviceRegistrationResponseGenerator
    {
        private readonly ITrustedDeviceAuthorizationCodeChallengeStore _codeChallengeStore;

        public TrustedDeviceRegistrationResponseGenerator(ITrustedDeviceAuthorizationCodeChallengeStore trustedDeviceAuthorizationCodeChallengeStore) {
            _codeChallengeStore = trustedDeviceAuthorizationCodeChallengeStore ?? throw new ArgumentNullException(nameof(trustedDeviceAuthorizationCodeChallengeStore));
        }

        public async Task<TrustedDeviceRegistrationResponse> Generate(TrustedDeviceRegistrationRequestValidationResult validationResult) {
            if (validationResult.InteractionMode == InteractionMode.Fingerprint) {
                return await GenerateFingerprintResponse(validationResult);
            }
            return await Task.FromResult<TrustedDeviceRegistrationResponse>(null);
        }

        private async Task<TrustedDeviceRegistrationResponse> GenerateFingerprintResponse(TrustedDeviceRegistrationRequestValidationResult validationResult) {
            var authorizationCode = new TrustedDeviceAuthorizationCode {
                ClientId = validationResult.Client.ClientId,
                CodeChallenge = validationResult.CodeChallenge,
                CodeChallengeMethod = validationResult.CodeChallengeMethod,
                CreationTime = DateTime.UtcNow.Date,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes,
                Subject = validationResult.Principal
            };
            var id = await _codeChallengeStore.Create(authorizationCode);
            return new TrustedDeviceRegistrationResponse {
                Challenge = id
            };
        }
    }
}
