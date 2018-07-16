using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Indice.Extensions
{
    /// <summary>
    /// Usful string method Extension
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Gets a random string. Nice for password generation.
        /// </summary>
        /// <param name="random">random instance</param>
        /// <param name="length">The length of the string</param>
        /// <returns></returns>
        public static string NextCode(this Random random, short length = 12) {
            var chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            var builder = new StringBuilder(length);

            for (var i = 0; i < length; ++i) {
                builder.Append(chars[random.Next(chars.Length)]);
            }

            return builder.ToString();
        }
        
        /// <summary>Transliterate Unicode string to ASCII string.</summary>
        /// <param name="input">String you want to transliterate into ASCII</param>
        /// <param name="tempStringBuilderCapacity">If you know the length of the result, pass the value for StringBuilder capacity. InputString.Length * 2 is used by default.</param>
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

        /// <summary>Transliterate Unicode character to ASCII string.</summary>
        /// <param name="character">Character you want to transliterate into ASCII</param>
        /// <returns>ASCII string. Unknown(?) unicode characters will return [?] (3 characters). It is this way in Python code as well.</returns>
        public static string Unidecode(this char character) {
            string result;

            if (character < 0x80) {
                result = new string(character, 1);
            } else {
                var high = character >> 8;
                var low = character & 0xff;

                if (Unidecoder.Characters.TryGetValue(high, out var transliterations)) {
                    result = transliterations[low];
                } else {
                    result = string.Empty;
                }
            }

            return result;
        }

#if !NETSTANDARD14
        /// <summary>
        /// Removes accent but keeps encoding.
        /// </summary>
        /// <param name="input">The string to manipulate</param>
        /// <returns></returns>
        public static string RemoveDiacritics(this string input) {
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString) {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
#endif
    }
}