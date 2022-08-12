using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Indice.Extensions;

namespace Indice.Types
{
    /// <summary>
    /// Creates and validates creditor reference (RF) payment codes based on ISO 11649, RF Creditor Reference.
    /// </summary>
    public class CreditorReference
    {
        #region Characters to number mapping
        private static readonly Dictionary<char, string> CharToNumberMapping = new() {
            { 'A', "10" },
            { 'B', "11" },
            { 'C', "12" },
            { 'D', "13" },
            { 'E', "14" },
            { 'F', "15" },
            { 'G', "16" },
            { 'H', "17" },
            { 'I', "18" },
            { 'J', "19" },
            { 'K', "20" },
            { 'L', "21" },
            { 'M', "22" },
            { 'N', "23" },
            { 'O', "24" },
            { 'P', "25" },
            { 'Q', "26" },
            { 'R', "27" },
            { 'S', "28" },
            { 'T', "29" },
            { 'U', "30" },
            { 'V', "31" },
            { 'W', "32" },
            { 'X', "33" },
            { 'Y', "34" },
            { 'Z', "35" }
        };
        #endregion

        internal CreditorReference(string creditorReference) {
            CheckDigits = creditorReference.Substring(2, 2);
            Reference = Regex.Replace(creditorReference.Substring(4, creditorReference.Length - 4), @"\s+", string.Empty).ToUpper();
        }

        internal CreditorReference(params string[] references) {
            if (TryGetInputReference(out var reference, references)) {
                Reference = reference.ToUpper();
                CheckDigits = CalculateCheckDigits(Reference);
            } else {
                throw new ArgumentException("Input references must have at least one element and sum up to 21 characters.", nameof(references));
            }
        }

        /// <summary>Creates a new instance of <see cref="CreditorReference"/>.</summary>
        /// <param name="references">
        /// The creditor reference may consist e.g. of the combination of a customer number and an invoice number. 
        /// It may additionally include an identification code for the entity concerned, in order to facilitate the selection of the correct ledger for reconciliation.
        /// </param>
        public static CreditorReference Create(params string[] references) => new(references);

        /// <summary>Two check digits calculated using algorithm ISO/IEC 7064.</summary>
        /// <remarks>https://www.iso.org/standard/31531.html</remarks>
        public string CheckDigits { get; }
        /// <summary>Proprietary creditor reference information.</summary>
        public string Reference { get; }
        /// <summary>Creditor reference in electronic format.</summary>
        public string ElectronicFormat => $"RF{CheckDigits}{Reference}";

        /// <summary>Tries to parse a given string as a creditor reference.</summary>
        /// <param name="creditorReference">The creditor reference to parse.</param>
        /// <param name="result">The creditor reference as a <see cref="CreditorReference"/> object.</param>
        /// <returns>Returns true if given string is valid, otherwise false.</returns>
        public static bool TryParse(string creditorReference, out CreditorReference result) {
            if (IsValid(creditorReference)) {
                result = new CreditorReference(creditorReference);
                return true;
            }
            result = null;
            return false;
        }

        /// <summary>Tries to parse a given string as a creditor reference.</summary>
        /// <param name="creditorReference">The creditor reference to parse.</param>
        /// <returns>The creditor reference as a <see cref="CreditorReference"/> object.</returns>
        /// <exception cref="FormatException">Parameter <paramref name="creditorReference"/> is not in the correct format.</exception>
        public static CreditorReference Parse(string creditorReference) {
            var isValid = TryParse(creditorReference, out var result);
            if (isValid) {
                return result;
            }
            throw new FormatException("Creditor reference is not in a valid format.");
        }

        /// <summary>Validates a creditor reference.</summary>
        /// <param name="creditorReference">The creditor reference to validate.</param>
        public static bool IsValid(string creditorReference) {
            creditorReference = Regex.Replace(creditorReference, @"\s+", string.Empty).ToUpper();
            var firstFourChars = creditorReference.Substring(0, 4);
            creditorReference = $"{creditorReference.Replace(firstFourChars, string.Empty)}{firstFourChars}";
            creditorReference = creditorReference.Select(x => x.IsLatinUpper() ? CharToNumberMapping[x] : x.ToString())
                                                 .Aggregate((current, next) => current + next);
            return BigInteger.TryParse(creditorReference, out var referenceNumber) && referenceNumber % 97 == 1;
        }

        /// <summary>Displays the creditor reference in it's print format, i.e RF78 MMKI DHR7 3738 3MLA KSI</summary>
        public override string ToString() => ElectronicFormat
            .Select((character, index) => (Character: character, Index: index))
            .GroupBy(x => x.Index / 4)
            .Select(group => group.Select(x => x.Character))
            .Select(characters => new string(characters.ToArray()))
            .Aggregate((current, next) => $"{current} {next}");

        /// <summary>Tries to validate and extract the creditor reference from user input.</summary>
        /// <param name="reference">The extracted creditor reference.</param>
        /// <param name="references">The input references.</param>
        /// <returns>Returns true if input references are valid, otherwise false.</returns>
        private static bool TryGetInputReference(out string reference, params string[] references) {
            reference = string.Empty;
            if (references?.Length == null || references.Length == 0) {
                return false;
            }
            reference = references.Aggregate((current, next) => current + next);
            // Reference must be up to 21 characters and consist of letters or digits.
            return reference.Length <= 21 && reference.All(x => x.IsDigit() || x.IsLatinLetter());
        }

        /// <remarks>https://docs.microsoft.com/en-us/dotnet/api/system.numerics.biginteger</remarks>
        private static string CalculateCheckDigits(string reference) {
            reference = reference.Select(x => x.IsLatinUpper() ? CharToNumberMapping[x] : x.ToString())
                                 .Aggregate((current, next) => current + next);
            reference = $"{reference}271500";
            var referenceNumber = BigInteger.Parse(reference);
            return Convert.ToInt32(98 - (int)(referenceNumber % 97)).ToString().PadLeft(2, '0');
        }
    }
}
