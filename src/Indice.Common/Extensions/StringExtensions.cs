using System;
using System.Linq;
using System.Text;

namespace Indice.Extensions
{
    public static class StringExtensions
    {
        public static string NextCode(this Random random, short length = 12) {
            var chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            var builder = new StringBuilder(length);

            for (var i = 0; i < length; ++i) {
                builder.Append(chars[random.Next(chars.Length)]);
            }

            return builder.ToString();
        }

        public static bool Contains(this string source, string toCheck, StringComparison stringComparison) => source?.IndexOf(toCheck, stringComparison) >= 0;

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

        public static byte[] ToByteArray(this string input) => Encoding.ASCII.GetBytes(input);
    }
}