using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using IdentityModel.Client;
using Indice.Extensions;
using Indice.Features.GovGr.Configuration;
using Indice.Features.GovGr.Interfaces;
using Indice.Features.GovGr.Models;
using Indice.Features.GovGr.Types;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;

namespace Indice.Features.GovGr;

/// <inheritdoc />
internal class GovGrKycClient : IKycService
{
    private const string FQDN_DEMO = "kycdemo.gsis.gr";
    private const string FQDN_STAGE = "kyc-stage.gov.gr";
    private const string FQDN_PROD = "kyc.gov.gr";
    private readonly HttpClient _httpClient;
    private readonly GovGrKycScopeDescriber _govGrKycScopeDescriber;
    private readonly GovGrOptions.KycOptions _settings;

    internal GovGrKycClient(
        HttpClient httpClient,
        GovGrKycScopeDescriber govGrKycScopeDescriber,
        GovGrOptions.KycOptions settings) {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _govGrKycScopeDescriber = govGrKycScopeDescriber ?? throw new ArgumentNullException(nameof(govGrKycScopeDescriber));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    protected string BaseDomain => _settings.IsStaging ? FQDN_STAGE :
                                   _settings.IsDevelopment ? FQDN_DEMO : FQDN_PROD;
    protected Uri BaseAddress => new($"https://{BaseDomain}");

    public List<ScopeDescription> GetAvailableScopes(IStringLocalizer localizer = null) => _govGrKycScopeDescriber.GetDescriptions(localizer);

    /// <summary>Get Data from eGov KYC</summary>
    public async Task<KycPayload> GetDataAsync(string code) {
        if (_settings.IsMock)
            return JsonSerializer.Deserialize<KycPayload>(GovGrConstants.KycMockJsonString);

        if (code is null) { throw new ArgumentNullException(nameof(code)); }

        // exchange authorization code with access token, using basic authentication 
        var accessToken = await GetAccessToken(code);

        _httpClient.SetBearerToken(accessToken);
        var httpResponse = await _httpClient.GetAsync($"{BaseAddress}/1/data");
        var response = await httpResponse.Content.ReadAsStringAsync();

        if (!httpResponse.IsSuccessStatusCode) {
            throw new GovGrServiceException(response);
        }
        var encodedResponse = JsonSerializer.Deserialize<KycHttpResponse>(response);
        var signatureVerified = await VerifySignature(encodedResponse);

        if (!signatureVerified) {
            throw new Exception("Signature could not be verified.");
        }
        var jsonString = encodedResponse.Payload.Base64UrlSafeDecode();
        return JsonSerializer.Deserialize<KycPayload>(jsonString);
    }

    /// <summary>Verify EGovKyc's Response Signature</summary>
    private async Task<bool> VerifySignature(KycHttpResponse kycHttpResponse) {
        // Decode Protected
        var protectedJsonString = kycHttpResponse.Protected.Base64UrlSafeDecode();
        // Deserialize decoded Protected
        var @protected = JsonSerializer.Deserialize<Protected>(protectedJsonString);

        // Get the Public Key of the Certificate that was used for signing the response
        var x5uHttpResponse = await _httpClient.GetAsync(@protected.X5u);
        var certificatePemString = await x5uHttpResponse.Content.ReadAsStringAsync();

        // convert certificate string into X509 certificate
        // https://stackoverflow.com/a/65352811/19162333
        certificatePemString = certificatePemString.Replace("-----BEGIN CERTIFICATE-----", null).Replace("-----END CERTIFICATE-----", null);
        var certificateByteArray = Convert.FromBase64String(certificatePemString);
        var certificate = new X509Certificate2(certificateByteArray);
        // use X509 certificate to create a signatureProvider
        var securityKey = new X509SecurityKey(certificate);
        var cryptoProviderFactory = securityKey.CryptoProviderFactory;
        var signatureProvider = cryptoProviderFactory.CreateForVerifying(securityKey, "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256");

        // Signature is BASE64URL Encoded!
        var signatureByteArray = Base64UrlEncoder.DecodeBytes(kycHttpResponse.Signature);
        // notice that we "JWT-concat" Protected & Payload!
        var token = $"{kycHttpResponse.Protected}.{kycHttpResponse.Payload}";
        var encodedByteArray = Encoding.UTF8.GetBytes(token);

        // Is signature valid?
        var isValid = signatureProvider.Verify(encodedByteArray, signatureByteArray);
        // cleanup...
        cryptoProviderFactory.ReleaseSignatureProvider(signatureProvider);
        return isValid;
    }

    /// <summary>Get Access Token from EGovKyc Identity server</summary>
    private async Task<string> GetAccessToken(string code) {
        var tokenResponse = await _httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest {
            Address = $"{BaseAddress}/oauth/token",
            ClientId = _settings.ClientId,
            ClientSecret = _settings.ClientSecret,
            RedirectUri = _settings.RedirectUri,
            Code = code,
        });
        if (tokenResponse.IsError) {
            throw new GovGrServiceException(tokenResponse.Error);
        }
        return tokenResponse.AccessToken;
    }
}
