using System.Text;
using System.Text.RegularExpressions;

namespace Indice.Features.Agents.Core.Workflows.Verification;

/// <summary>
/// Compares free-text typed by the user against the <see cref="VerifiableField"/>s of a customer data
/// payload, without ever disclosing them. The assistant asks "tell me your license plate or your home
/// address"; this decides whether the answer matches something already on file.
/// </summary>
/// <remarks>
/// Comparison is deliberately forgiving about formatting (spacing, punctuation, casing, phone country code)
/// and strict about content: a value matches only when it appears in full inside the answer.
/// </remarks>
public static partial class VerificationMatcher
{
    /// <summary>Number of trailing digits a phone number is compared by, so <c>+30 210 1234567</c> matches <c>2101234567</c>.</summary>
    private const int PhoneSignificantDigits = 9;

    /// <summary>Shortest code-like value accepted as a substring match; shorter values must match a whole token.</summary>
    private const int MinimumSubstringLength = 4;

    /// <summary>Returns the canonical comparison form of <paramref name="field"/>.</summary>
    public static string Normalize(VerifiableField field) => Normalize(field.Kind, field.Value);

    /// <summary>Returns the canonical comparison form of <paramref name="value"/> read as <paramref name="kind"/>.</summary>
    public static string Normalize(VerifiableFieldKind kind, string value) => kind switch {
        VerifiableFieldKind.Email => value.Trim().ToLowerInvariant(),
        VerifiableFieldKind.Phone => LastDigits(value),
        _ => Squash(value)
    };

    /// <summary>
    /// Returns the first field of <paramref name="fields"/> that <paramref name="answer"/> proves knowledge of,
    /// or <c>null</c> when the answer matches nothing. Fields are probed in the order supplied, so callers can
    /// prioritise the kind they challenged for.
    /// </summary>
    public static VerifiableField? Match(IEnumerable<VerifiableField> fields, string? answer) {
        if (string.IsNullOrWhiteSpace(answer)) {
            return null;
        }
        return fields.FirstOrDefault(field => Matches(field, answer));
    }

    /// <summary>Whether <paramref name="answer"/> contains the value of <paramref name="field"/>.</summary>
    public static bool Matches(VerifiableField field, string answer) {
        if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(field.Value)) {
            return false;
        }
        switch (field.Kind) {
            case VerifiableFieldKind.Email:
                return answer.Contains(field.Value.Trim(), StringComparison.OrdinalIgnoreCase);
            case VerifiableFieldKind.Phone:
                var expected = LastDigits(field.Value);
                return expected.Length >= 6 && DigitRuns(answer).Any(run => LastDigits(run) == expected);
            case VerifiableFieldKind.PostalAddress:
                // Addresses are typed loosely ("Ermou 12" vs "12 Ermou str., Athens"): require at least two
                // significant tokens of the stored address — and at least half of them — to appear in the
                // answer, ignoring order and filler words. A single common token is never enough.
                var tokens = Tokenize(field.Value);
                if (tokens.Count == 0) {
                    return false;
                }
                var answerTokens = Tokenize(answer).ToHashSet(StringComparer.Ordinal);
                var hits = tokens.Count(answerTokens.Contains);
                return hits >= 2 && hits * 2 >= tokens.Count;
            default:
                var value = Squash(field.Value);
                if (value.Length == 0) {
                    return false;
                }
                return value.Length >= MinimumSubstringLength
                    ? Squash(answer).Contains(value, StringComparison.Ordinal)
                    : Tokenize(answer).Contains(value);
        }
    }

    /// <summary>Uppercase, alphanumeric-only projection of <paramref name="value"/> — drops spacing and punctuation noise.</summary>
    private static string Squash(string value) {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Trim().ToUpperInvariant()) {
            if (char.IsLetterOrDigit(character)) {
                builder.Append(character);
            }
        }
        return builder.ToString();
    }

    /// <summary>Significant uppercase tokens of <paramref name="value"/>: alphanumeric runs of two characters or more.</summary>
    private static List<string> Tokenize(string value) => [..
        NonAlphanumericRegex().Split(value.ToUpperInvariant())
            .Where(token => token.Length > 1)
    ];

    /// <summary>The trailing significant digits of a phone number, so country-code prefixes do not defeat the comparison.</summary>
    private static string LastDigits(string value) {
        var digits = new string([.. value.Where(char.IsAsciiDigit)]);
        return digits.Length <= PhoneSignificantDigits ? digits : digits[^PhoneSignificantDigits..];
    }

    /// <summary>The digit runs (allowing inner separators) found in a free-text answer.</summary>
    private static IEnumerable<string> DigitRuns(string answer) => PhoneRegex().Matches(answer).Select(match => match.Value);

    [GeneratedRegex(@"[^A-Z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphanumericRegex();

    [GeneratedRegex(@"\+?[\d][\d\s().-]{5,20}\d", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
