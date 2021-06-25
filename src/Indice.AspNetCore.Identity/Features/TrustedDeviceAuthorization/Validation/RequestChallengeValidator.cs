using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Extensions;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;
using Microsoft.IdentityModel.Tokens;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Validation
{
    internal class RequestChallengeValidator
    {
        protected static ValidationResult ValidateSignature(string publicKey, string codeChallenge, string signature) {
            var isSupportedKey = publicKey.IsCertificate() || publicKey.IsPublicKey() || publicKey.IsRSAPublicKey();
            if (!isSupportedKey) {
                return new ValidationResult {
                    IsError = true,
                    Error = OidcConstants.TokenErrors.InvalidGrant,
                    ErrorDescription = "Public key algorithm is not supported."
                };
            }
            SecurityKey securityKey;
            if (publicKey.IsCertificate()) {
                var certificate = new X509Certificate2(Convert.FromBase64String(publicKey.TrimPublicKeyHeaders()));
                securityKey = new X509SecurityKey(certificate);
            } else {
                var rsa = RSA.Create();
                if (publicKey.IsRSAPublicKey()) {
                    rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey.TrimPublicKeyHeaders()), out _);
                } else {
                    rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey.TrimPublicKeyHeaders()), out _);
                }
                securityKey = new RsaSecurityKey(rsa) {
                    CryptoProviderFactory = new CryptoProviderFactory {
                        CacheSignatureProviders = false
                    }
                };
            }
            if (!ValidateChallengeAgainstSignature(codeChallenge, signature, securityKey)) {
                return new ValidationResult {
                    IsError = true,
                    Error = OidcConstants.TokenErrors.InvalidGrant,
                    ErrorDescription = "Code challenge does not match against security key."
                };
            }
            return Success();
        }

        protected static bool ValidateChallengeAgainstSignature(string codeChallenge, string signature, SecurityKey securityKey) {
            var cryptoProviderFactory = securityKey.CryptoProviderFactory;
            var signatureProvider = cryptoProviderFactory.CreateForVerifying(securityKey, "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");
            var canVerifyCodeChallenge = signatureProvider.Verify(Encoding.UTF8.GetBytes(codeChallenge), Convert.FromBase64String(signature));
            cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
            return canVerifyCodeChallenge;
        }

        protected static ValidationResult ValidateAuthorizationCodeWithProofKeyParameters(string codeVerifier, TrustedDeviceAuthorizationCode authorizationCode) {
            if (string.IsNullOrWhiteSpace(authorizationCode.CodeChallenge)) {
                return new ValidationResult {
                    IsError = true,
                    Error = OidcConstants.TokenErrors.InvalidGrant,
                    ErrorDescription = "Client is missing code challenge."
                };
            }
            if (!ValidateCodeVerifierAgainstCodeChallenge(codeVerifier, authorizationCode.CodeChallenge)) {
                return new ValidationResult {
                    IsError = true,
                    Error = OidcConstants.TokenErrors.InvalidGrant,
                    ErrorDescription = "Transformed code verifier does not match code challenge."
                };
            }
            return Success();
        }

        protected static bool ValidateCodeVerifierAgainstCodeChallenge(string codeVerifier, string codeChallenge) {
            var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
            var hashedBytes = codeVerifierBytes.Sha256();
            var transformedCodeVerifier = Base64Url.Encode(hashedBytes);
            // https://github.com/IdentityModel/IdentityModel/blob/main/src/TimeConstantComparer.cs
            return TimeConstantComparer.IsEqual(transformedCodeVerifier.Sha256(), codeChallenge);
        }

        protected static ValidationResult Invalid(string errorDescription = null) => new() {
            IsError = true,
            Error = OidcConstants.TokenErrors.InvalidGrant,
            ErrorDescription = errorDescription
        };

        protected static ValidationResult Success() => new() {
            IsError = false
        };
    }
}
