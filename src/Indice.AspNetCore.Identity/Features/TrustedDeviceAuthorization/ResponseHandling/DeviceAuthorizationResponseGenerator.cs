using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation;
using Microsoft.AspNetCore.Authentication;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal class DeviceAuthorizationResponseGenerator : IResponseGenerator<DeviceAuthorizationRequestValidationResult, DeviceAuthorizationResponse>
    {
        public DeviceAuthorizationResponseGenerator(
            IAuthorizationCodeChallengeStore codeChallengeStore,
            ISystemClock systemClock
        ) {
            CodeChallengeStore = codeChallengeStore ?? throw new ArgumentNullException(nameof(codeChallengeStore));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public IAuthorizationCodeChallengeStore CodeChallengeStore { get; }
        public ISystemClock SystemClock { get; }

        public async Task<DeviceAuthorizationResponse> Generate(DeviceAuthorizationRequestValidationResult validationResult) {
            var authorizationCode = new TrustedDeviceAuthorizationCode {
                ClientId = validationResult.Client.ClientId,
                CodeChallenge = validationResult.CodeChallenge.Sha256(),
                CreationTime = SystemClock.UtcNow.UtcDateTime,
                DeviceId = validationResult.Device.DeviceId,
                InteractionMode = validationResult.InteractionMode,
                Lifetime = validationResult.Client.AuthorizationCodeLifetime,
                RequestedScopes = validationResult.RequestedScopes,
                Subject = Principal.Create("TrustedDevice", new Claim(JwtClaimTypes.Subject, validationResult.UserId))
            };
            var challenge = await CodeChallengeStore.GenerateChallenge(authorizationCode);
            return new DeviceAuthorizationResponse {
                Challenge = challenge
            };
        }
    }
}
