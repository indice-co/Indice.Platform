using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Indice.Extensions;

namespace Indice.Types
{
    /// <summary>
    /// 
    /// </summary>
    public class CreditorReference
    {
        #region Characters to number mapping
        private static readonly Dictionary<char, string> _charToNumberMapping = new() {
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

        internal CreditorReference(params string[] references) {
            if (TryGetInputReference(out var reference, references)) {
                Reference = reference.ToUpper();
                CheckDigits = CalculateCheckDigits(Reference);
            } else {
                throw new ArgumentException("Input references must have at least one element and sum up to 21 characters.", nameof(references));
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="CreditorReference"/>.
        /// </summary>
        /// <param name="references">
        /// The creditor reference may consist e.g. of the combination of a customer number and an invoice number. It may additionally include an identification code for the entity concerned, 
        /// in order to facilitate the selection of the correct ledger for reconciliation.
        /// </param>
        public static CreditorReference Create(params string[] references) => new(references);

        /// <summary>
        /// Two check digits calculated using algorithm ISO/IEC 7064.
        /// </summary>
        /// <remarks>https://www.iso.org/standard/31531.html</remarks>
        public int CheckDigits { get; }
        /// <summary>
        /// Proprietary creditor reference information.
        /// </summary>
        public string Reference { get; }
        /// <summary>
        /// Creditor reference in electronic format.
        /// </summary>
        public string ElectronicFormat => $"RF{CheckDigits}{Reference}";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creditorReference"></param>
        /// <returns></returns>
        public static bool IsValid(string creditorReference) {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ToString() {
            return base.ToString();
        }

        /// <summary>
        /// Tries to validate and extract the creditor reference from user input.
        /// </summary>
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
        private static int CalculateCheckDigits(string reference) {
            reference = reference.Select(x => x.IsLatinUpper() ? _charToNumberMapping[x] : x.ToString())
                                 .Aggregate((current, next) => current + next);
            reference = $"{reference}271500";
            var referenceNumber = BigInteger.Parse(reference);
            return Convert.ToInt32(98 - (int)(referenceNumber % 97));
        }
    }
}
