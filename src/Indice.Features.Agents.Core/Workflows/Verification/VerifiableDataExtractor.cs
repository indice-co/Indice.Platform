using System.Text.Json;
using System.Text.RegularExpressions;

namespace Indice.Features.Agents.Core.Workflows.Verification;

/// <summary>
/// Walks an arbitrary customer data payload (JSON returned by an MCP tool) and collects the leaf values a
/// person can be challenged against — plates, phone numbers, e-mail addresses, addresses, customer codes.
/// Classification is name-driven first (the property name states what the value is) and falls back to the
/// value's own shape, so it works against payloads this assembly knows nothing about.
/// </summary>
public static partial class VerifiableDataExtractor
{
    /// <summary>Values shorter than this carry too little entropy to verify anyone with.</summary>
    private const int MinimumValueLength = 3;

    /// <summary>Depth guard for pathological payloads.</summary>
    private const int MaximumDepth = 8;

    /// <summary>Collects the verifiable fields of <paramref name="payload"/>, in document order and de-duplicated by kind + normalized value.</summary>
    public static IReadOnlyList<VerifiableField> Extract(JsonElement payload) {
        var fields = new List<VerifiableField>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Visit(payload, path: string.Empty, propertyName: string.Empty, depth: 0, fields, seen);
        return fields;
    }

    private static void Visit(JsonElement element, string path, string propertyName, int depth, List<VerifiableField> fields, HashSet<string> seen) {
        if (depth > MaximumDepth) {
            return;
        }
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject()) {
                    Visit(property.Value, Combine(path, property.Name), property.Name, depth + 1, fields, seen);
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray()) {
                    // Keep the owning property name so items of e.g. "phoneNumbers" are still classified as phones.
                    Visit(item, Combine(path, index.ToString()), propertyName, depth + 1, fields, seen);
                    index++;
                }
                break;
            case JsonValueKind.String:
            case JsonValueKind.Number:
                var value = element.ToString();
                if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < MinimumValueLength) {
                    return;
                }
                if (Classify(propertyName, value.Trim()) is not { } kind) {
                    return;
                }
                var field = new VerifiableField(path, kind, value.Trim());
                if (seen.Add($"{kind}|{VerificationMatcher.Normalize(field)}")) {
                    fields.Add(field);
                }
                break;
        }
    }

    private static string Combine(string path, string segment) => string.IsNullOrEmpty(path) ? segment : $"{path}.{segment}";

    /// <summary>Maps a property name and its value onto a <see cref="VerifiableFieldKind"/>, or <c>null</c> when the value is not usable for verification.</summary>
    private static VerifiableFieldKind? Classify(string propertyName, string value) {
        var name = propertyName.Replace("_", string.Empty).Replace("-", string.Empty);
        if (Contains(name, "email") || Contains(name, "mail")) {
            return EmailRegex().IsMatch(value) ? VerifiableFieldKind.Email : null;
        }
        if (Contains(name, "phone") || Contains(name, "mobile") || Contains(name, "msisdn") || Contains(name, "telephone") || Contains(name, "tel")) {
            return LooksLikePhone(value) ? VerifiableFieldKind.Phone : null;
        }
        if (Contains(name, "plate") || Contains(name, "registrationnumber") || Contains(name, "vehiclenumber")) {
            return VerifiableFieldKind.LicensePlate;
        }
        if (Contains(name, "address") || Contains(name, "street")) {
            return VerifiableFieldKind.PostalAddress;
        }
        if (Contains(name, "taxid") || Contains(name, "taxnumber") || Contains(name, "vat") || Contains(name, "afm")) {
            return VerifiableFieldKind.TaxId;
        }
        if (Contains(name, "customercode") || Contains(name, "customerid") || Contains(name, "customernumber")) {
            return VerifiableFieldKind.CustomerCode;
        }
        if (Contains(name, "contract") || Contains(name, "accountnumber") || Contains(name, "policynumber")) {
            return VerifiableFieldKind.ContractNumber;
        }
        if (Contains(name, "casenumber") || Contains(name, "caseid") || Contains(name, "referencenumber")) {
            return VerifiableFieldKind.CaseNumber;
        }
        // The name said nothing; fall back to unambiguous value shapes only.
        if (EmailRegex().IsMatch(value)) {
            return VerifiableFieldKind.Email;
        }
        return null;
    }

    private static bool Contains(string name, string token) => name.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePhone(string value) {
        var digits = value.Count(char.IsAsciiDigit);
        return digits is >= 8 and <= 15 && value.All(c => char.IsAsciiDigit(c) || c is '+' or '-' or ' ' or '(' or ')' or '.');
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}
