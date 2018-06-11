using System;
using System.Text;

namespace Indice.Extensions
{
    /// <summary>
    /// Extension methods regarding Url processing.
    /// </summary>
    public static class UrlExtensions
    {
        static readonly char[] padding = { '=' };

        public static string ToBase64UrlSafe(this byte[] bytes) {
            return Convert.ToBase64String(bytes).TrimEnd(padding).Replace('+', '-').Replace('/', '_'); ;
        }
        public static string Base64UrlSafeEncode(this string plain) {
            return ToBase64UrlSafe(Encoding.UTF8.GetBytes(plain));
        }
        public static string ToBase64StringSafe(this byte[] bytes) {
            return Convert.ToBase64String(bytes);
        }
        public static string Base64StringSafeEncode(this string plain) {
            return ToBase64StringSafe(Encoding.UTF8.GetBytes(plain));
        }
        public static byte[] FromBase64UrlSafe(this string encoded) {
            string incoming = encoded.Replace('_', '/').Replace('-', '+');
            switch (encoded.Length % 4) {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(incoming);
            return bytes;
        }

        public static string Base64UrlSafeDecode(this string encoded) {
            var bytes = FromBase64UrlSafe(encoded);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }
    }
}
