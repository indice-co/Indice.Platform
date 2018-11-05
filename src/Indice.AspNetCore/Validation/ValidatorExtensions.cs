using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace Indice.Validation
{
    /// <summary>
    /// Extensions that enhance the fluent validation with additional validator methods.
    /// </summary>
    public static class ValidatorExtensions
    {
        /// <summary>
        /// Check the taxid number for format &amp; checksum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder"></param>
        /// <param name="countryISOAccessor">A lambda that on the same object that will give the country two letter iso (ie GR)</param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> TaxCode<T>(this IRuleBuilder<T, string> ruleBuilder, Func<T, string> countryISOAccessor) =>
            ruleBuilder.Must((entity, taxCode) => ValidTaxCode(countryISOAccessor(entity), taxCode)).WithMessage(ValidatorMessages.INVALID_TAXCODE);

        /// <summary>
        /// Check the taxid number for format &amp; checksum.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ruleBuilder"></param>
        /// <param name="countryISO">the country two letter iso (ie GR)</param>
        /// <returns></returns>
        public static IRuleBuilderOptions<T, string> TaxCode<T>(this IRuleBuilder<T, string> ruleBuilder, string countryISO) => ruleBuilder.TaxCode(_ => countryISO);

        private static bool ValidTaxCode(string countryISO, string taxCode) {
            if (string.IsNullOrEmpty(taxCode) || string.IsNullOrEmpty(countryISO)) {
                return true;
            }

            try {
                return TaxCodeValidator.CheckNumber(taxCode, countryISO);
            } catch (NotSupportedException) {
                return true; // Do not valdate unsupported VAT country codes.
            }
        }
    }
}
