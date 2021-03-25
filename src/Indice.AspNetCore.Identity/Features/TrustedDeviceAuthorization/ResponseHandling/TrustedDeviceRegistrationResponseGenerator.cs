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
            var id = await _codeChallengeStore.Store(new TrustedDeviceAuthorizationCode { 
                ClientId = validationResult.Client.ClientId,
                CreationTime = DateTime.UtcNow.Date,
                Subject = validationResult.Principal,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes
            });
            return new TrustedDeviceRegistrationResponse { 
                Challenge = id
            };
        }
    }
}
