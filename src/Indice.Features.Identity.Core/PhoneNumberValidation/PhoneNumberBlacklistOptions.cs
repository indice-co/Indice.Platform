namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides configuration options for phone number blacklist validation.
/// </summary>
public sealed class PhoneNumberBlacklistOptions
{
    /// <summary>
    /// The configuration section name used to bind these options.
    /// </summary>
    public const string SectionName = "PhoneNumberBlacklist";

    /// <summary>
    /// Gets or sets a value indicating whether phone number blacklist validation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a comma-separated list of additional phone numbers to blacklist.
    /// </summary>
    public string? Numbers { get; set; }
}
