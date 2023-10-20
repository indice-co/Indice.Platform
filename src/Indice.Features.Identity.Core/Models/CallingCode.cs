namespace Indice.Features.Identity.Core.Models;
/// <summary> The calling code for a supported country. </summary>
public class CallingCode
{
    private const string PlusSign = "+";

    /// <summary> Constructs a new instance of <see cref="CallingCode"/></summary>
    public CallingCode(string code, string twoLetterCode, string countryName, string pattern) {
        Code = code;
        TwoLetterCountryCode = twoLetterCode;
        CountryName = countryName;
        Pattern = pattern;
    }

    /// <summary>The calling code.</summary>
    public string Code { get; private set; }
    /// <summary>The calling code display name.</summary>
    public string DisplayName => $"{CountryName} ({PlusSign}{Code})";
    /// <summary> The two letter country code for the corresponding country. </summary>
    public string TwoLetterCountryCode { get; private set; }
    /// <summary>The country name.</summary>
    public string CountryName { get; private set; }
    /// <summary>Regex pattern for phone number validation.</summary>
    public string Pattern { get; private set; }

    /// <inheritdoc/>
    public override string ToString() => $"{PlusSign}{Code}";
}
