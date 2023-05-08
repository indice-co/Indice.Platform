using FluentValidation;

namespace Indice.Validation;

/// <summary>Extensions that enhance the fluent validation with additional validator methods.</summary>
public static class ValidatorExtensions
{
    /// <summary>Check the tax id number for format &amp; checksum.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="countryISOAccessor">A lambda that on the same object that will give the country two letter ISO code (i.e GR).</param>
    public static IRuleBuilderOptions<T, string> TaxCode<T>(this IRuleBuilder<T, string> ruleBuilder, Func<T, string> countryISOAccessor) =>
        ruleBuilder.Must((entity, taxCode) => ValidTaxCode(countryISOAccessor(entity), taxCode)).WithMessage(ValidatorMessages.INVALID_TAXCODE);

    /// <summary>Check the tax id number for format &amp; checksum.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    /// <param name="countryISO">The country two letter ISO code (i.e GR).</param>
    public static IRuleBuilderOptions<T, string> TaxCode<T>(this IRuleBuilder<T, string> ruleBuilder, string countryISO) => ruleBuilder.TaxCode(_ => countryISO);

    /// <summary>Validates a Greek phone number. Should start with '69' and be 10 characters long.</summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="ruleBuilder">Rule builder.</param>
    public static IRuleBuilderOptions<T, string> GreekPhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder) => 
        ruleBuilder.Must(phoneNumber => string.IsNullOrEmpty(phoneNumber) || phoneNumber.StartsWith("69")).Length(10);

    private static bool ValidTaxCode(string countryISO, string taxCode) {
        if (string.IsNullOrEmpty(taxCode) || string.IsNullOrEmpty(countryISO)) {
            return true;
        }
        try {
            return TaxCodeValidator.CheckNumber(taxCode, countryISO);
        } catch (NotSupportedException) {
            return true; // Do not validate unsupported VAT country codes.
        }
    }
}
