namespace Indice.Features.Identity.Core.Configuration;

/// <summary> Configures the validation about the supported countries.</summary>
public class SupportedCountry {
    /// <summary> The two letter code for each supported country. </summary>
    public string TwoLetterCode { get; set; }
    /// <summary> The Regex pattern for phone validation for each supported country. </summary>
    public string Pattern { get; set; }
}
