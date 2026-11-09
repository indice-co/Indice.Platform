using Indice.Globalization;
using Microsoft.Extensions.Options;

namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides additional blacklisted phone numbers from application configuration.
/// </summary>
public sealed class ConfigurationPhoneNumberBlacklistProvider : IPhoneNumberBlacklistProvider
{
    private readonly HashSet<string> _blacklist;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationPhoneNumberBlacklistProvider"/> class.
    /// </summary>
    /// <param name="options">The phone number blacklist configuration options.</param>
    public ConfigurationPhoneNumberBlacklistProvider(IOptions<PhoneNumberBlacklistOptions> options) {
        _blacklist = options.Value.Numbers?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(x => x is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal)
            ?? [];
    }

    /// <inheritdoc/>
    public bool IsPhoneNumberBlacklisted(string phoneNumber) =>
        _blacklist.Contains(phoneNumber);

    private static string? Normalize(string value) =>
        PhoneNumber.TryParse(value, out var normalized)
            ? normalized.ToString("O")
            : null;
}