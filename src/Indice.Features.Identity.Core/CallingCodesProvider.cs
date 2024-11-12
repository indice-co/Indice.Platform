using Indice.Features.Identity.Core.Configuration;
using Indice.Features.Identity.Core.Models;
using Indice.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Indice.Features.Identity.Core;
/// <summary> Provides the application supported countries. </summary>
public class CallingCodesProvider
{
    private readonly List<SupportedCountry> _supportedCountries;

    /// <summary>Creates a new instance of <see cref="CallingCodesProvider"/>.</summary>
    /// <param name="configuration">The application configuration.</param>
    public CallingCodesProvider(IConfiguration configuration) {
        _supportedCountries = configuration.GetSection($"{nameof(IdentityOptions)}:User:PhoneNumberCountries").Get<List<SupportedCountry>>() ?? new List<SupportedCountry>();
    }

    /// <summary> Retrieves all the supported countries. Defaults to GR if not configured. </summary>
    /// <returns>A list of <see cref="CountryInfo"/> with the configured default countries.</returns>
    public List<CallingCode> GetSupportedCallingCodes() {
        var supportedCountries = new List<CallingCode>();
        foreach (var country in _supportedCountries) {
            if (CountryInfo.TryGetCountryByNameOrCode(country.TwoLetterCode, out var countryInfo)) {
                foreach(var callingCode in countryInfo!.CallingCode.Split(',')) {
                    supportedCountries.Add(new CallingCode(callingCode, countryInfo.TwoLetterCode, countryInfo.Name, country.Pattern));
                }
            }
        }
        if (!supportedCountries.Any()) {
            if (CountryInfo.TryGetCountryByNameOrCode("GR", out var countryInfo)) {
                foreach (var callingCode in countryInfo!.CallingCode.Split(',')) {
                    supportedCountries.Add(new CallingCode(callingCode, countryInfo.TwoLetterCode, countryInfo.Name, string.Empty));
                }
            }
        }
        return supportedCountries;
    }

    /// <summary>Retrieves info about a calling code.</summary>
    /// <param name="code">The calling code.</param>
    /// <returns>A <see cref="CallingCode"/> object with the required information.</returns>
    public CallingCode? GetCallingCode(string code) {
        if (string.IsNullOrWhiteSpace(code)) {
            return null;
        }
        if (!CountryInfo.TryGetCountryByCallingCode(code, out var countryInfo)) {
            return null;
        }
        var configuredCountry = _supportedCountries?.FirstOrDefault(c => c.TwoLetterCode == countryInfo!.TwoLetterCode);
        if (configuredCountry == null && countryInfo!.TwoLetterCode != "GR") {
            return null;
        }
        return new CallingCode(code, countryInfo!.TwoLetterCode, countryInfo.Name, configuredCountry?.Pattern);
    }
}
