using System.Text.RegularExpressions;
using Indice.Globalization;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides a default implementation of <see cref="IPhoneNumberValidator"/>.
/// The validator will use the 'IdentityOptions.User.PhoneNUmberRegex' to validate the phone number.
/// If the regex is not provided then by default this will validate only Greek phone numbers without the country code.
/// </summary>
public class DefaultPhoneNumberValidator : IPhoneNumberValidator
{
    private readonly IConfiguration configuration;
    private static readonly Lazy<CountryInfo[]> countries;

    static DefaultPhoneNumberValidator() {
        countries = new Lazy<CountryInfo[]>(static () => new[] { CountryInfo.GetCountryByNameOrCode("GR") });
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultPhoneNumberValidator"/>.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public DefaultPhoneNumberValidator(IConfiguration configuration) {
        this.configuration = configuration;
    }

    /// <inheritdoc/>
    public CountryInfo[] GetCountries() {
        return countries.Value;
    }

    /// <inheritdoc/>
    public bool Validate(string phoneNumber) {
        var phoneNumberRegex = configuration.GetIdentityOption("User", "PhoneNumberRegex");

        return string.IsNullOrEmpty(phoneNumberRegex)
            ? phoneNumber is { Length: 10 } && phoneNumber.StartsWith("69") // Greek phone number default.
            : Regex.IsMatch(phoneNumber, phoneNumberRegex);
    }
}
