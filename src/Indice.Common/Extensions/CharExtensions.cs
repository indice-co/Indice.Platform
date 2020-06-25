namespace Indice.Extensions
{
    /// <summary>
    /// Extensions methods on <see cref="char"/> struct.
    /// </summary>
    public static class CharExtensions
    {
        /// <summary>Transliterate unicode character to ASCII string.</summary>
        /// <param name="character">Character you want to transliterate into ASCII.</param>
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

        /// <summary>
        /// Checks if the specified character is a digit.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if character is digit, otherwise false.</returns>
        /// <remarks>https://www.cs.cmu.edu/~pattis/15-1XX/common/handouts/ascii.html</remarks>
        public static bool IsDigit(this char character) {
            var charAsciiDecimal = (int)character;
            return charAsciiDecimal >= 48 && charAsciiDecimal <= 57;
        }

        /// <summary>
        /// Checks if the specified character is an uppercase English letter.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if character is upper, otherwise false.</returns>
        /// <remarks>https://www.cs.cmu.edu/~pattis/15-1XX/common/handouts/ascii.html</remarks>
        public static bool IsLatinUpper(this char character) {
            var charAsciiDecimal = (int)character;
            return charAsciiDecimal >= 65 && charAsciiDecimal <= 90;
        }

        /// <summary>
        /// Checks if the specified character is a lowercase English letter.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if character is lower, otherwise false.</returns>
        /// <remarks>https://www.cs.cmu.edu/~pattis/15-1XX/common/handouts/ascii.html</remarks>
        public static bool IsLatinLower(this char character) {
            var charAsciiDecimal = (int)character;
            return charAsciiDecimal >= 97 && charAsciiDecimal <= 122;
        }

        /// <summary>
        /// Checks if the specified character is a special character.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if character is special, otherwise false.</returns>
        /// <remarks>https://www.cs.cmu.edu/~pattis/15-1XX/common/handouts/ascii.html</remarks>
        public static bool IsSpecial(this char character) {
            var charAsciiDecimal = (int)character;
            return (charAsciiDecimal >= 33 && charAsciiDecimal <= 47) ||
                   (charAsciiDecimal >= 58 && charAsciiDecimal <= 64) ||
                   (charAsciiDecimal >= 91 && charAsciiDecimal <= 96) ||
                   (charAsciiDecimal >= 123 && charAsciiDecimal <= 126);
        }

        /// <summary>
        /// Checks if the specified character is an English letter (uppercase or lowercase).
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns>Returns true if character is letter, otherwise false.</returns>
        public static bool IsLatinLetter(this char character) => character.IsLatinUpper() || character.IsLatinLower();
    }
}
