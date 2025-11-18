using System.Globalization;

namespace Indice.Localization;

/// <summary>
/// Temporarily sets the current thread's culture and UI culture to the specified culture.
/// </summary>
public sealed class TemporaryCulture : IDisposable
{
    private readonly CultureInfo? _originalCulture;
    private readonly CultureInfo? _originalUICulture;
    private bool _restoredCulture;

    /// <summary>
    /// Sets the current thread's culture and UI culture to the specified culture.
    /// </summary>
    /// <param name="cultureName">Culture name (e.g., "fr-FR", "en-US").</param>
    public TemporaryCulture(string? cultureName) {
        if (string.IsNullOrWhiteSpace(cultureName)) {
            _restoredCulture = true;
            return;
        }
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;

        CultureInfo newCulture = new CultureInfo(cultureName!);
        CultureInfo.CurrentCulture = newCulture;
        CultureInfo.CurrentUICulture = newCulture;
    }

    /// <summary>
    /// Restores the original culture settings.
    /// </summary>
    public void Dispose() {
        if (!_restoredCulture) {
            CultureInfo.CurrentCulture = _originalCulture!;
            CultureInfo.CurrentUICulture = _originalUICulture!;
            _restoredCulture = true;
        }
    }
}
