namespace Indice.Features.Identity.Core.Configuration;
/// <summary>Configuration about the supported countries.</summary>
public class PhoneNumberOptions
{
    /// <summary>The name is used to mark the section found inside a configuration file.</summary>
    public static readonly string Name = "PhoneNumber";

    /// <summary> A list of the supported countries. </summary>
    public List<SupportedCountry> SupportedCountries { get; set; } = new();
}

/// <summary> Configures the validation about the supported countries.</summary>
public class SupportedCountry {
    /// <summary> The two letter code for each supported country. </summary>
    public string TwoLetterCode { get; set; }
    /// <summary> The Regex pattern for phone validation for each supported country. </summary>
    public string Pattern { get; set; }
}
