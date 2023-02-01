namespace Indice.Features.Identity.Core.DeviceAuthentication.Extensions
{
    internal static class StringExtensions
    {
        // Headers for certificates: PEM format - Base64 encoded ASCII.
        private const string CertificateHeader = "-----BEGIN CERTIFICATE-----";
        private const string CertificateFooter = "-----END CERTIFICATE-----";
        // Headers for public keys, without specifying the algorithm identifier.
        private const string PublicKeyHeader = "-----BEGIN PUBLIC KEY-----";
        private const string PublicKeyFooter = "-----END PUBLIC KEY-----";
        // Headers for RSA public keys.
        private const string RSAPublicKeyHeader = "-----BEGIN RSA PUBLIC KEY-----";
        private const string RSAPublicKeyFooter = "-----END RSA PUBLIC KEY-----";

        public static string TrimPublicKeyHeaders(this string publicKey) {
            return publicKey
                .Replace(CertificateHeader, string.Empty).Replace(CertificateFooter, string.Empty)
                .Replace(PublicKeyHeader, string.Empty).Replace(PublicKeyFooter, string.Empty)
                .Replace(RSAPublicKeyHeader, string.Empty).Replace(RSAPublicKeyFooter, string.Empty);
        }

        public static bool IsRSAPublicKey(this string publicKey) => publicKey.StartsWith(RSAPublicKeyHeader) && publicKey.TrimLineEndings().EndsWith(RSAPublicKeyFooter);

        public static bool IsPublicKey(this string publicKey) => publicKey.StartsWith(PublicKeyHeader) && publicKey.TrimLineEndings().EndsWith(PublicKeyFooter);

        public static bool IsCertificate(this string publicKey) => publicKey.StartsWith(CertificateHeader) && publicKey.TrimLineEndings().EndsWith(CertificateFooter);

        private static string TrimLineEndings(this string text) => text.TrimEnd('\r', '\n', ' ');
    }
}
