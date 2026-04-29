#if NET9_0_OR_GREATER
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Indice.AspNetCore.Http.Headers;

/// <summary>
/// Provides functionality to convert HTTP header values containing forwarded client certificate
/// information into <see cref="X509Certificate2"/> objects.
/// </summary>
/// <remarks>
/// This class supports both Envoy-style ("x-forwarded-client-cert") and NGINX-style ("ssl-client-cert")
/// forwarded certificate headers. For Envoy, it supports both structured key/value pairs (containing
/// a <c>Cert</c> field with URL-encoded PEM data) and raw PEM-encoded certificate values.
/// </remarks>
public static class CertificateForwardingHeaderConverters
{
    /// <summary>
    /// Represents the HTTP header name used by Envoy to forward client certificate information.
    /// </summary>
    public const string EnvoyClientCertificateHeaderName = "x-forwarded-client-cert";
    /// <summary>
    /// Represents the HTTP header name used by NGINX to forward client certificate information.
    /// </summary>
    public const string NginxClientCertificateHeaderName = "ssl-client-cert";

    /// <summary>
    /// Converts the value of the Envoy "x-forwarded-client-cert" header into an <see cref="X509Certificate2"/> object.
    /// Supports both structured key/value format (e.g., <c>Hash=...;Cert=...;Chain=...</c>) and raw PEM-encoded values.
    /// </summary>
    /// <param name="headerValue">The value of the "x-forwarded-client-cert" header.</param>
    /// <returns>An <see cref="X509Certificate2"/> object representing the client certificate,
    /// or <see langword="null"/> if <paramref name="headerValue"/> is null or whitespace.</returns>
    /// <exception cref="CertificateForwardingHeaderParseException">Thrown when the header value cannot be parsed into a valid certificate.</exception>
    public static X509Certificate2? ConvertFromEnvoyHeader(string headerValue) {
        if (string.IsNullOrWhiteSpace(headerValue)) {
            return null;
        }
        // Support raw PEM format (detect BEGIN CERTIFICATE marker)
        if (headerValue.Contains("-----BEGIN CERTIFICATE-----")) {
            try {
                return X509Certificate2.CreateFromPem(Uri.UnescapeDataString(headerValue));
            } catch (Exception ex) {
                throw new CertificateForwardingHeaderParseException("Failed to parse certificate from Envoy header raw PEM value.", ex);
            }
        }
        // Parse the semicolon-separated structured parts (e.g., Hash=...;Cert=...;Chain=...)
        if (!headerValue.Contains('=') && !headerValue.Contains(';')) {
            throw new CertificateForwardingHeaderParseException("Certificate header value is not in a recognized format (expected structured key=value pairs or raw PEM).");
        }
        try {
            var parts = headerValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => {
                                    var idx = s.IndexOf('=');
                                    if (idx < 0) return new KeyValuePair<string, string>(s, string.Empty);
                                    return new KeyValuePair<string, string>(s[..idx], s[(idx + 1)..].Trim('"'));
                                })
                                .ToDictionary(StringComparer.OrdinalIgnoreCase);
            if (!parts.TryGetValue("Cert", out var certificatePart)) {
                throw new CertificateForwardingHeaderParseException("Certificate header does not contain a 'Cert' field.");
            }
            return X509Certificate2.CreateFromPem(Uri.UnescapeDataString(certificatePart));
        } catch (CertificateForwardingHeaderParseException) {
            throw;
        } catch (Exception ex) {
            throw new CertificateForwardingHeaderParseException("Failed to parse certificate from Envoy header.", ex);
        }
    }

    /// <summary>
    /// Converts the value of the NGINX "ssl-client-cert" header into an <see cref="X509Certificate2"/> object.
    /// The header value is expected to be a URL-encoded PEM-encoded certificate.
    /// </summary>
    /// <param name="headerValue">The value of the "ssl-client-cert" header.</param>
    /// <returns>An <see cref="X509Certificate2"/> object representing the client certificate,
    /// or <see langword="null"/> if <paramref name="headerValue"/> is null or whitespace.</returns>
    /// <exception cref="CertificateForwardingHeaderParseException">Thrown when the header value cannot be parsed into a valid certificate.</exception>
    public static X509Certificate2? ConvertFromNginxHeader(string headerValue) {
        if (string.IsNullOrWhiteSpace(headerValue)) {
            return null;
        }
        try {
            return X509Certificate2.CreateFromPem(WebUtility.UrlDecode(headerValue));
        } catch (Exception ex) {
            throw new CertificateForwardingHeaderParseException("Failed to parse certificate from NGINX header.", ex);
        }
    }
}

/// <summary>
/// Exception thrown when the configured forwarded header cannot be parsed into a valid certificate.
/// </summary>
public class CertificateForwardingHeaderParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the CertificateForwardingHeaderParseException class with a specified error
    /// message.    
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CertificateForwardingHeaderParseException(string message) : this(message, null) { }

    /// <summary>
    /// Initializes a new instance of the CertificateForwardingHeaderParseException class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception. 
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is
    /// specified.</param>
    public CertificateForwardingHeaderParseException(string message, Exception? innerException) : base(message, innerException) { }
}
#endif