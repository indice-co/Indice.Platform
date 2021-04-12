using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Indice.AspNetCore.Identity.Features
{
    internal class CompleteRegistrationRequestValidator
    {
        private readonly ILogger<InitRegistrationRequestValidator> _logger;

        public CompleteRegistrationRequestValidator(ILogger<CompleteRegistrationRequestValidator> logger) {

        }

        public async Task<CompleteRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            _logger.LogDebug("[CompleteRegistrationRequestValidator] Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            //var tokenResult = await _tokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            //if (tokenResult.IsError) {
            //    return Error(tokenResult.Error, "Provided access token is not valid.");
            //}
            //// The access token must have a 'sub' claim.
            //var subjectClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.Subject);
            //if (subjectClaim == null) {
            //    return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{nameof(JwtClaimTypes.Subject)}' claim.");
            //}
            //// The access token must have a 'client_id' claim.
            //var clientIdClaim = tokenResult.Claims.SingleOrDefault(claim => claim.Type == JwtClaimTypes.ClientId);
            //if (clientIdClaim == null) {
            //    return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{nameof(JwtClaimTypes.ClientId)}' claim.");
            //}
            //// Check if the consumer specified the desired interaction.
            //var modeString = parameters.Get(TrustedDeviceRegistrationRequest.Mode);
            //var mode = TrustedDeviceRegistrationRequest.GetInteractionMode(modeString);
            //if (!mode.HasValue) {
            //    return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(TrustedDeviceRegistrationRequest.Mode)}' used for registration (fingerprint or 4pin) is not specified.");
            //}
            //// Check if the consumer specified the device id.
            //var deviceId = parameters.Get(TrustedDeviceRegistrationRequest.DeviceId);
            //if (string.IsNullOrWhiteSpace(deviceId)) {
            //    return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{nameof(TrustedDeviceRegistrationRequest.DeviceId)}' is not specified.");
            //}
            return new CompleteRegistrationRequestValidationResult { };
        }
    }
}
