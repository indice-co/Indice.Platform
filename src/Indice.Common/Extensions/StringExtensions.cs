using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Indice.Extensions;

/// <summary>Extensions methods on <see cref="string"/> type.</summary>
public static partial class StringExtensions
{
    /// <summary>Gets a random string. Nice for password generation.</summary>
    /// <param name="random">Instance of <see cref="Random"/> type.</param>
    /// <param name="length">The length of the string.</param>
    public static string NextCode(this Random random, short length = 12) {
        var chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        var builder = new StringBuilder(length);
        for (var i = 0; i < length; ++i) {
            builder.Append(chars[random.Next(chars.Length)]);
        }
        return builder.ToString();
    }

    /// <summary>Transliterate unicode string to ASCII string.</summary>
    /// <param name="input">String you want to transliterate into ASCII.</param>
    /// <param name="tempStringBuilderCapacity">If you know the length of the result, pass the value for <see cref="StringBuilder"/> capacity. <paramref name="input"/>.Length * 2 is used by default.</param>
    /// <returns>ASCII string. There are [?] (3 characters) in places of some unknown(?) unicode characters. It is this way in Python code as well.</returns>
    public static string Unidecode(this string input, int? tempStringBuilderCapacity = null) {
        if (string.IsNullOrEmpty(input)) {
            return string.Empty;
        }
        if (input.All(x => x < 0x80)) {
            return input;
        }
        // Unidecode result often can be at least two times longer than input string.
        var stringBuilder = new StringBuilder(tempStringBuilderCapacity ?? input.Length * 2);
        foreach (var character in input) {
            // Copy-paste is bad, but stringBuilder.Append(character.Unidecode()); would be a bit slower.
            if (character < 0x80) {
                stringBuilder.Append(character);
            } else {
                var high = character >> 8;
                var low = character & 0xff;
                if (Unidecoder.Characters.TryGetValue(high, out var transliterations)) {
                    stringBuilder.Append(transliterations[low]);
                }
            }
        }
        return stringBuilder.ToString();
    }

    /// <summary>Converts a string to kebab case.</summary>
    /// <param name="value">The string to kebaberize.</param>
    public static string ToKebabCase(this string value) {
        // Find and replace all parts that starts with one capital letter e.g. Net
        value = WordsRegex().Replace(value, m => $"{(m.Groups["delimiter"].Success ? m.Groups["delimiter"].Value : "-")}{m.Groups["word"].Value.ToLower()}");
        // Find and replace all parts that are all capital letter e.g. NET
        value = WordsAllCapsRegex().Replace(value, m => $"{(m.Groups["delimiter"].Success ? m.Groups["delimiter"].Value : "-")}{m.Groups["word"].ToString().ToLower()}");
        
        // Return.
        return value.TrimStart('-');
    }

#if NET7_0_OR_GREATER
    /// <summary>Match all parts of a sentence that start with one capital letter e.g. Net</summary>
    [GeneratedRegex(@"(?<delimiter>[/\\-])?(?<word>[A-Z]?[a-z0-9.]+)[,;|\s]*")]
    private static partial Regex WordsRegex();
    /// <summary>Match all parts of a sentence that are all capital letter e.g. NET</summary>
    [GeneratedRegex(@"(?<delimiter>[/\\-])?(?<word>[A-Z][A-Z0-9.]*)[,;|\s]*")]
    private static partial Regex WordsAllCapsRegex();
#else
    private static readonly Regex _wordsRegex = new(@"(?<delimiter>[/\\-])?(?<word>[A-Z]?[a-z0-9.]+)[,;|\s]*");
    private static readonly Regex _wordsAllCapsRegex = new(@"(?<delimiter>[/\\-])?(?<word>[A-Z][A-Z0-9.]*)[,;|\s]*");
    /// <summary>Match all parts of a sentence that start with one capital letter e.g. Net</summary>
    private static Regex WordsRegex() => _wordsRegex;
    /// <summary>Match all parts of a sentence that are all capital letter e.g. NET</summary>
    private static Regex WordsAllCapsRegex() => _wordsAllCapsRegex;
#endif

#if !NETSTANDARD14
    /// <summary>Removes accent but keeps encoding.</summary>
    /// <param name="input">The string to manipulate.</param>
    public static string RemoveDiacritics(this string input) {
        var normalizedString = input.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();
        foreach (var @char in normalizedString) {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(@char);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                stringBuilder.Append(@char);
            }
        }
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
#endif
}