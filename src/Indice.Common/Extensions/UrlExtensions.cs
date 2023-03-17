using System.Text;

namespace Indice.Extensions;

/// <summary>Extension methods regarding URL processing.</summary>
public static class UrlExtensions
{
    static readonly char[] padding = { '=' };

    /// <summary>Converts the specified URL safe string, which encodes binary data as base-64 digits, to an equivalent 8-bit unsigned integer array.</summary>
    /// <param name="encoded">The string to convert.</param>
    public static byte[] FromBase64UrlSafe(this string encoded) {
        var incoming = encoded.Replace('_', '/').Replace('-', '+');
        switch (encoded.Length % 4) {
            case 2: incoming += "=="; break;
            case 3: incoming += "="; break;
        }
        var bytes = Convert.FromBase64String(incoming);
        return bytes;
    }

    /// <summary>Converts an array of 8-bit unsigned integers to its equivalent string representation, suitable to be used in a URL, that is encoded with base-64 digits.</summary>
    /// <param name="bytes">An array of 8-bit unsigned integers.</param>
    public static string ToBase64UrlSafe(this byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd(padding).Replace('+', '-').Replace('/', '_');

    /// <summary>Converts a string to its equivalent string representation, suitable to be used in a URL, that is encoded with base-64 digits.</summary>
    /// <param name="plain">The string to convert.</param>
    public static string Base64UrlSafeEncode(this string plain) => ToBase64UrlSafe(Encoding.UTF8.GetBytes(plain));

    /// <summary>Converts a string, which encodes binary data as base-64 digits, to its equivalent string representation that is encoded with base-64 digits.</summary>
    /// <param name="encoded">The string to convert.</param>
    public static string Base64UrlSafeDecode(this string encoded) {
        var bytes = FromBase64UrlSafe(encoded);
        return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    }

    /// <summary>Converts an array of 8-bit unsigned integers to its equivalent string representation that is encoded with base-64 digits.</summary>
    /// <param name="bytes">An array of 8-bit unsigned integers.</param>
    public static string ToBase64StringSafe(this byte[] bytes) => Convert.ToBase64String(bytes);

    /// <summary>Converts a string to its equivalent string representation that is encoded with base-64 digits.</summary>
    /// <param name="plain">The string to convert.</param>
    public static string Base64StringSafeEncode(this string plain) => ToBase64StringSafe(Encoding.UTF8.GetBytes(plain));
}
