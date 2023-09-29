using Indice.Globalization;

namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides an abstraction for validating phone numbers.
/// </summary>
public interface IPhoneNumberValidator
{
    /// <summary>
    /// Returns the two-letter ISO codes of supported countries.
    /// </summary>
    CountryInfo[] GetCountries();

    /// <summary>
    /// Validates a phone number.
    /// </summary>
    bool Validate(string phoneNumber);
}
