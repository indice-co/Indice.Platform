using System.Globalization;
using System.Text;

namespace Indice.Features.Agents.Core.Services.Validation;

/// <summary>
/// Default <see cref="IChallengeValidator"/> that compares user input with the case field value, ignoring case, accents,
/// separators (spaces, dashes, etc.) and whether Greek or Latin characters were used.
/// </summary>
public sealed class DefaultChallengeValidator : IChallengeValidator
{
    /// <summary>The key of the fallback validator used when no validator is registered for a case type.</summary>
    public const string Default = "default";
    /// <inheritdoc/>
    public bool IsValid(string userInput, string expectedValue) {
        if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(expectedValue))
            return false;
        var normalizedInput = NormalizeForComparison(userInput);
        var normalizedActual = NormalizeForComparison(expectedValue);
        return normalizedInput.Length > 0 && string.Equals(normalizedInput, normalizedActual, StringComparison.Ordinal);
    }

    /// <summary>
    /// Greek letters mapped to their Latin equivalent. Letters that look alike on the keyboard (e.g. Ρ, Η, Χ) map to
    /// their look-alike so that values such as plate numbers match regardless of the keyboard layout used.
    /// </summary>
    private static readonly Dictionary<char, string> GreekToLatin = new() {
        ['Α'] = "A", ['Β'] = "B", ['Γ'] = "G", ['Δ'] = "D", ['Ε'] = "E", ['Ζ'] = "Z", ['Η'] = "H", ['Θ'] = "TH",
        ['Ι'] = "I", ['Κ'] = "K", ['Λ'] = "L", ['Μ'] = "M", ['Ν'] = "N", ['Ξ'] = "KS", ['Ο'] = "O", ['Π'] = "P",
        ['Ρ'] = "P", ['Σ'] = "S", ['Τ'] = "T", ['Υ'] = "Y", ['Φ'] = "F", ['Χ'] = "X", ['Ψ'] = "PS", ['Ω'] = "O"
    };

    /// <summary>Upper-cases, strips accents and non alphanumeric characters, and transliterates Greek to ASCII.</summary>
    private static string NormalizeForComparison(string value) {
        var decomposed = value.ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed) {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) {
                continue;
            }
            if (GreekToLatin.TryGetValue(c, out var latin)) {
                builder.Append(latin);
            } else if (char.IsAsciiLetterOrDigit(c)) {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
}
