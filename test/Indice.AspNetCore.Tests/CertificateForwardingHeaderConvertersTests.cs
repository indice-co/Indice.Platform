#if NET9_0_OR_GREATER
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Indice.AspNetCore.Http.Headers;
using Xunit;

namespace Indice.AspNetCore.Tests;

public class CertificateForwardingHeaderConvertersTests
{
    private static X509Certificate2 CreateTestCertificate() {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    // ── ConvertFromEnvoyHeader ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertFromEnvoyHeader_ReturnsNull_WhenHeaderIsNullOrWhiteSpace(string? headerValue) {
        var result = CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(headerValue!);
        Assert.Null(result);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_ParsesRawPem() {
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();

        using var result = CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(pem);

        Assert.NotNull(result);
        Assert.Equal(cert.Thumbprint, result.Thumbprint);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_ParsesStructuredEnvoyFormat() {
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();
        var encodedCert = Uri.EscapeDataString(pem);
        var headerValue = $"Hash=abc123;Cert={encodedCert};Chain=xyz";

        using var result = CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(headerValue);

        Assert.NotNull(result);
        Assert.Equal(cert.Thumbprint, result.Thumbprint);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_ParsesStructuredEnvoyFormat_WithOnlyCertField() {
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();
        var encodedCert = Uri.EscapeDataString(pem);
        var headerValue = $"Cert={encodedCert}";

        using var result = CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(headerValue);

        Assert.NotNull(result);
        Assert.Equal(cert.Thumbprint, result.Thumbprint);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_ParsesStructuredEnvoyFormat_CertValueWithBase64PaddingEquals() {
        // URL-encoding preserves the semantics; verify key=value split uses only the first '='
        // so that URL-encoded '=' (%3D) in the cert base64 body does not break parsing.
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();
        // URL-encode the PEM so that embedded '=' chars become %3D (as Envoy actually does)
        var encodedCert = Uri.EscapeDataString(pem);
        // Sanity: the encoded value must not contain literal '=' (confirming we test the right thing)
        Assert.DoesNotContain("=", encodedCert);
        var headerValue = $"Hash=fingerprint;Cert={encodedCert}";

        using var result = CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(headerValue);

        Assert.NotNull(result);
        Assert.Equal(cert.Thumbprint, result.Thumbprint);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_Throws_WhenHeaderHasNoEqualsOrSemicolon() {
        Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader("notavalidheader"));
    }

    [Fact]
    public void ConvertFromEnvoyHeader_Throws_WhenCertFieldIsMissing() {
        var ex = Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader("Hash=abc123;Subject=CN=test"));
        Assert.Contains("Cert", ex.Message);
    }

    [Fact]
    public void ConvertFromEnvoyHeader_Throws_WhenCertValueIsInvalidPem() {
        var headerValue = "Cert=notavalidpem";
        Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader(headerValue));
    }

    [Fact]
    public void ConvertFromEnvoyHeader_ExceptionMessage_DoesNotContainHeaderValue() {
        // Verify certificate data does not leak into exception messages
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();
        var ex = Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromEnvoyHeader($"Hash=abc;Cert=BROKENPEM"));
        Assert.DoesNotContain("BROKENPEM", ex.Message);
    }

    // ── ConvertFromNginxHeader ─────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertFromNginxHeader_ReturnsNull_WhenHeaderIsNullOrWhiteSpace(string? headerValue) {
        var result = CertificateForwardingHeaderConverters.ConvertFromNginxHeader(headerValue!);
        Assert.Null(result);
    }

    [Fact]
    public void ConvertFromNginxHeader_ParsesUrlEncodedPem() {
        using var cert = CreateTestCertificate();
        var pem = cert.ExportCertificatePem();
        var encodedPem = WebUtility.UrlEncode(pem);

        using var result = CertificateForwardingHeaderConverters.ConvertFromNginxHeader(encodedPem);

        Assert.NotNull(result);
        Assert.Equal(cert.Thumbprint, result.Thumbprint);
    }

    [Fact]
    public void ConvertFromNginxHeader_Throws_WhenValueIsInvalidPem() {
        Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromNginxHeader("notvalidpem"));
    }

    [Fact]
    public void ConvertFromNginxHeader_ExceptionMessage_DoesNotContainHeaderValue() {
        // Verify certificate data does not leak into exception messages
        const string invalidValue = "BROKENPEMDATA";
        var ex = Assert.Throws<CertificateForwardingHeaderParseException>(
            () => CertificateForwardingHeaderConverters.ConvertFromNginxHeader(invalidValue));
        Assert.DoesNotContain(invalidValue, ex.Message);
    }
}
#endif
