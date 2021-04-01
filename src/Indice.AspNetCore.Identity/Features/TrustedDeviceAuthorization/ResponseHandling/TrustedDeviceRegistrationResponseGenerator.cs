using System;
using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal class TrustedDeviceRegistrationResponseGenerator : ITrustedDeviceRegistrationResponseGenerator
    {
        private readonly ITrustedDeviceAuthorizationCodeChallengeStore _codeChallengeStore;

        public TrustedDeviceRegistrationResponseGenerator(
            ITrustedDeviceAuthorizationCodeChallengeStore trustedDeviceAuthorizationCodeChallengeStore
        ) {
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
                CreationTime = DateTime.UtcNow.Date,
                Subject = validationResult.Principal,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes
            };
            var id = await _codeChallengeStore.Create(authorizationCode);
            return new TrustedDeviceRegistrationResponse {
                Challenge = id
            };
        }
    }
}
