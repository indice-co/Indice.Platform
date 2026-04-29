#if NET9_0_OR_GREATER
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Indice.Extensions;

namespace Indice.AspNetCore.Http.Headers;

/// <summary>
/// Provides functionality to convert the value of the "x-forwarded-client-cert" HTTP header into an X509Certificate2
/// object representing the forwarded client certificate.
/// </summary>
/// <remarks>This class is intended for use in scenarios where client certificates are forwarded by a proxy (such
/// as Envoy) using the "x-forwarded-client-cert" header. It supports parsing structured forwarded certificate header
/// values, such as Envoy-style key/value pairs containing fields like <c>Cert</c>. Use this class when you need to
/// extract and validate client certificates from HTTP headers in environments where direct TLS termination is not
public static class CertificateForwardingHeaderConverters
public class CertificateForwardingHeaderConverters
{
    /// <summary>
    /// Represents the HTTP header name used to forward client certificate information in proxy scenarios.  
    /// </summary>
    /// <remarks>This constant is typically used when working with reverse proxies that forward client
    /// certificate details to backend services. The header value may contain information about the client certificate
    /// presented to the proxy.</remarks>
    public const string Envoy_ClientCertificateHeaderName = "x-forwarded-client-cert";
    /// <summary>
    /// Represents the HTTP header name used to forward client certificate information in proxy scenarios.  
    /// </summary>
    /// <remarks>This constant is typically used when working with reverse proxies that forward client
    /// certificate details to backend services. The header value may contain information about the client certificate
    /// presented to the proxy.</remarks>
    public const string NGINX_ClientCertificateHeaderName = "ssl-client-cert";

    /// <summary>
    /// Converts the value of the "x-forwarded-client-cert" header into an X509Certificate2 object. 
    /// </summary>
    /// <param name="headerValue">The value of the "x-forwarded-client-cert" header.</param>
    /// <returns>An X509Certificate2 object representing the client certificate.</returns>
    /// <exception cref="CertificateForwardingHeaderParseException">Thrown when the header value cannot be parsed into a valid certificate.</exception>
    public static X509Certificate2 ConvertFromEnvoyHeader(string headerValue) {
        X509Certificate2? clientCertificate = null;
        if (string.IsNullOrWhiteSpace(headerValue)) {
            return clientCertificate!;
        }
        // Parse the semicolon-separated parts (Hash, Cert, Chain)
        if (!headerValue.Contains('=') && !headerValue.Contains(';')) {
            throw new CertificateForwardingHeaderParseException($"Certifiacate header is invalid HeaderValue: '{headerValue.Truncate(100)}'");
        }
        try {
            var parts = headerValue.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => new KeyValuePair<string, string>(s.Split("=")[0], s.Split("=")[1]?.Trim('"') ?? string.Empty))
                                .ToDictionary(StringComparer.OrdinalIgnoreCase);
            if (parts.Keys.Count >= 2 && parts.TryGetValue("Cert", out var certificatePart)) {
                clientCertificate = X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(Uri.UnescapeDataString(certificatePart)));
            }
        } catch (Exception ex) {
            throw new CertificateForwardingHeaderParseException($"Failed to parse certificate from header {headerValue.Truncate(200)}.", ex);
        }
        return clientCertificate!;
    }

    /// <summary>
    /// Converts the value of the "x-forwarded-client-cert" header into an X509Certificate2 object. 
    /// </summary>
    /// <param name="headerValue">The value of the "x-forwarded-client-cert" header.</param>
    /// <returns>An X509Certificate2 object representing the client certificate.</returns>
    /// <exception cref="CertificateForwardingHeaderParseException">Thrown when the header value cannot be parsed into a valid certificate.</exception>
    public static X509Certificate2 ConvertFromNginxHeader(string headerValue) {
        X509Certificate2? clientCertificate = null;
        if (!string.IsNullOrWhiteSpace(headerValue)) {
            clientCertificate = X509Certificate2.CreateFromPem(
                WebUtility.UrlDecode(headerValue));
        }
        return clientCertificate!;
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