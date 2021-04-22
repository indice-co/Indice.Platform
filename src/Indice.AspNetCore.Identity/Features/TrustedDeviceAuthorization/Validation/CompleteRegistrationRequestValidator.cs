using System;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.Data.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Configuration;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores;
using Indice.Extensions;
using Indice.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class CompleteRegistrationRequestValidator : RegistrationRequestValidatorBase<CompleteRegistrationRequestValidationResult>
    {
        private Client _client;

        public CompleteRegistrationRequestValidator(
            IAuthorizationCodeChallengeStore authorizationCodeChallengeStore,
            IClientStore clientStore,
            ILogger<CompleteRegistrationRequestValidator> logger,
            ISystemClock systemClock,
            ITokenValidator tokenValidator,
            ITotpService totpService
        ) : base(clientStore, tokenValidator) {
            AuthorizationCodeChallengeStore = authorizationCodeChallengeStore;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            SystemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
            TotpService = totpService ?? throw new ArgumentNullException(nameof(totpService));
        }

        public IAuthorizationCodeChallengeStore AuthorizationCodeChallengeStore { get; }
        public ILogger<CompleteRegistrationRequestValidator> Logger { get; }
        public ISystemClock SystemClock { get; }
        public ITotpService TotpService { get; }

        public override async Task<CompleteRegistrationRequestValidationResult> Validate(string accessToken, NameValueCollection parameters) {
            Logger.LogDebug($"[{nameof(CompleteRegistrationRequestValidator)}] Started trusted device registration request validation.");
            // The access token needs to be valid and have at least the openid scope.
            var tokenValidationResult = await TokenValidator.ValidateAccessTokenAsync(accessToken, IdentityServerConstants.StandardScopes.OpenId);
            if (tokenValidationResult.IsError) {
                return Error(tokenValidationResult.Error, "Provided access token is not valid.");
            }
            // The access token must have a 'sub' and 'client_id' claim.
            var claimsToValidate = new[] { JwtClaimTypes.Subject, JwtClaimTypes.ClientId };
            foreach (var claim in claimsToValidate) {
                var claimValue = tokenValidationResult.Claims.SingleOrDefault(x => x.Type == claim)?.Value;
                if (string.IsNullOrWhiteSpace(claimValue)) {
                    return Error(OidcConstants.ProtectedResourceErrors.InvalidToken, $"Access token must contain the '{claim}' claim.");
                }
            }
            // Validate that the consumer specified the 'mode', 'device_id', 'code_signature', 'code_verifier', 'public_key', 'code' and 'otp' parameters.
            var parametersToValidate = new[] {
                RegistrationRequestParameters.CodeSignature,
                RegistrationRequestParameters.CodeVerifier,
                RegistrationRequestParameters.PublicKey,
                RegistrationRequestParameters.OtpCode,
                RegistrationRequestParameters.Code
            };
            foreach (var parameter in parametersToValidate) {
                var parameterValue = parameters.Get(parameter);
                if (string.IsNullOrWhiteSpace(parameterValue)) {
                    return Error(OidcConstants.TokenErrors.InvalidRequest, $"Parameter '{parameter}' is not specified.");
                }
            }
            // Load client and validate that it allows the 'password' flow.
            _client = await LoadClient(tokenValidationResult);
            if (_client == null) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client is unknown or not enabled.");
            }
            if (!_client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword)) {
                return Error(OidcConstants.AuthorizeErrors.UnauthorizedClient, $"Client not authorized for 'password' grant type.");
            }
            // Validate authorization code against code verifier given by the client.
            var code = parameters.Get(RegistrationRequestParameters.Code);
            var codeVerifier = parameters.Get(RegistrationRequestParameters.CodeVerifier);
            var authorizationCode = await AuthorizationCodeChallengeStore.GetAuthorizationCode(code);
            var authorizationCodeValidationResult = await ValidateAuthorizationCode(code, authorizationCode, codeVerifier);
            if (authorizationCodeValidationResult.IsError) {
                return Error(authorizationCodeValidationResult.Error, authorizationCodeValidationResult.ErrorDescription);
            }
            // Validate given public key against signature.
            var publicKey = parameters.Get(RegistrationRequestParameters.PublicKey);
            var codeSignature = parameters.Get(RegistrationRequestParameters.CodeSignature);
            var publicKeyValidationResult = ValidateSignature(publicKey, code, codeSignature);
            if (publicKeyValidationResult.IsError) {
                return Error(publicKeyValidationResult.Error, publicKeyValidationResult.ErrorDescription);
            }
            // Find requested scopes.
            var requestedScopes = tokenValidationResult.Claims.Where(claim => claim.Type == JwtClaimTypes.Scope).Select(claim => claim.Value).ToList();
            // Create principal from incoming access token excluding protocol claims.
            var claims = tokenValidationResult.Claims.Where(x => !ProtocolClaimsFilter.Contains(x.Type));
            var principal = Principal.Create("TrustedDevice", claims.ToArray());
            var userId = tokenValidationResult.Claims.Single(x => x.Type == JwtClaimTypes.Subject).Value;
            // Validate OTP code.
            var totpResult = await TotpService.Verify(
                principal: principal,
                code: parameters.Get(RegistrationRequestParameters.OtpCode),
                purpose: Constants.TrustedDeviceOtpPurpose(userId, authorizationCode.DeviceId)
            );
            if (!totpResult.Success) {
                return Error(totpResult.Error);
            }
            // Finally return result.
            return new CompleteRegistrationRequestValidationResult {
                IsError = false,
                Client = _client,
                DeviceId = authorizationCode.DeviceId,
                DeviceName = parameters.Get(RegistrationRequestParameters.DeviceName),
                InteractionMode = authorizationCode.InteractionMode,
                Principal = principal,
                RequestedScopes = requestedScopes,
                UserId = userId
            };
        }

        public static ValidationResult ValidateSignature(string publicKey, string codeChallenge, string signature) {
            var certificate = new X509Certificate2(Convert.FromBase64String(publicKey.Replace("-----BEGIN CERTIFICATE-----", string.Empty).Replace("-----END CERTIFICATE-----", string.Empty)));
            var securityKey = new X509SecurityKey(certificate);
            if (!ValidateChallengeAgainstSignature(codeChallenge, signature, securityKey)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Transformed code verifier does not match code challenge.");
            }
            return Success();
        }

        private static bool ValidateChallengeAgainstSignature(string codeChallenge, string signature, SecurityKey securityKey) {
            var cryptoProviderFactory = securityKey.CryptoProviderFactory;
            var signatureProvider = cryptoProviderFactory.CreateForVerifying(securityKey, "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
            var canVerifyCodeChallenge = signatureProvider.Verify(Encoding.UTF8.GetBytes(codeChallenge), Convert.FromBase64String(signature));
            cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
            return canVerifyCodeChallenge;
        }

        private async Task<ValidationResult> ValidateAuthorizationCode(string code, TrustedDeviceAuthorizationCode authorizationCode, string codeVerifier) {
            if (authorizationCode == null) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Authorization code is invalid.");
            }
            // Validate that the current client is not trying to use an authorization code of a different client.
            if (authorizationCode.ClientId != _client.ClientId) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Authorization code is invalid.");
            }
            // Remove authorization code.
            await AuthorizationCodeChallengeStore.RemoveAuthorizationCode(code);
            // Validate code expiration.
            if (authorizationCode.CreationTime.HasExceeded(authorizationCode.Lifetime, SystemClock.UtcNow.UtcDateTime)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Authorization code is invalid.");
            }
            if (authorizationCode.CreationTime.HasExceeded(_client.AuthorizationCodeLifetime, SystemClock.UtcNow.UtcDateTime)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Authorization code is invalid.");
            }
            if (authorizationCode.RequestedScopes == null || !authorizationCode.RequestedScopes.Any()) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Authorization code is invalid.");
            }
            var proofKeyParametersValidationResult = ValidateAuthorizationCodeWithProofKeyParameters(codeVerifier, authorizationCode);
            if (proofKeyParametersValidationResult.IsError) {
                return Error(proofKeyParametersValidationResult.Error, proofKeyParametersValidationResult.ErrorDescription);
            }
            return Success();
        }

        private ValidationResult ValidateAuthorizationCodeWithProofKeyParameters(string codeVerifier, TrustedDeviceAuthorizationCode authorizationCode) {
            if (string.IsNullOrWhiteSpace(authorizationCode.CodeChallenge) || string.IsNullOrWhiteSpace(authorizationCode.CodeChallengeMethod)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, $"Client '{_client.ClientId}' is missing code challenge or code challenge method.");
            }
            if (!Constants.SupportedCodeChallengeMethods.Contains(authorizationCode.CodeChallengeMethod)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Unsupported code challenge method.");
            }
            if (!ValidateCodeVerifierAgainstCodeChallenge(codeVerifier, authorizationCode.CodeChallenge)) {
                return Error(OidcConstants.TokenErrors.InvalidGrant, "Transformed code verifier does not match code challenge.");
            }
            return Success();
        }

        private static bool ValidateCodeVerifierAgainstCodeChallenge(string codeVerifier, string codeChallenge) {
            var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hashedBytes = codeVerifierBytes.Sha256();
            var transformedCodeVerifier = Base64Url.Encode(hashedBytes);
            // https://github.com/IdentityModel/IdentityModel/blob/main/src/TimeConstantComparer.cs
            return TimeConstantComparer.IsEqual(transformedCodeVerifier.Sha256(), codeChallenge);
        }

        private static ValidationResult Success() => new() { IsError = false };
    }
}
