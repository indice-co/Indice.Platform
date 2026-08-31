namespace Indice.Features.Identity.Core.PhoneNumberValidation;

/// <summary>
/// Provides blacklisted phone numbers from the embedded Castle disposable phone number list.
/// </summary>
public sealed class CastlePhoneNumberBlacklistProvider : IPhoneNumberBlacklistProvider
{
    private const string ResourceName = "Indice.Features.Identity.Core.PhoneNumberValidation.phones-blacklist.conf";

    private readonly HashSet<string> _blacklist;

    /// <summary>
    /// Initializes a new instance of the <see cref="CastlePhoneNumberBlacklistProvider"/> class
    /// and loads the embedded phone number blacklist.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the embedded blacklist resource cannot be found.
    /// </exception>
    public CastlePhoneNumberBlacklistProvider() {
        var assembly = typeof(CastlePhoneNumberBlacklistProvider).Assembly;

        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded resource not found: {ResourceName}");

        using var reader = new StreamReader(stream);

        _blacklist = reader
            .ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(x => x is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public bool IsPhoneNumberBlacklisted(string phoneNumber) =>
        _blacklist.Contains(phoneNumber);

    private static string? Normalize(string value) =>
        PhoneNumberNormalizer.TryNormalize(value, out var normalized)
            ? normalized
            : null;
}