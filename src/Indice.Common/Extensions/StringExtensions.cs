using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Indice.Extensions
{
    /// <summary>
    /// Extensions methods on <see cref="string"/> type.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Gets a random string. Nice for password generation.
        /// </summary>
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

        /// <summary>
        /// Converts a string to kebab case.
        /// </summary>
        /// <param name="value">The string to kebaberize.</param>
        public static string ToKebabCase(this string value) {
            // Find and replace all parts that starts with one capital letter e.g. Net
            value = Regex.Replace(value, "[A-Z][a-z]+", m => $"-{m.ToString().ToLower()}");
            // Find and replace all parts that are all capital letter e.g. NET
            value = Regex.Replace(value, "[A-Z]+", m => $"-{m.ToString().ToLower()}");
            // Return.
            return value.TrimStart('-');
        }

#if !NETSTANDARD14
        /// <summary>
        /// Removes accent but keeps encoding.
        /// </summary>
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
}