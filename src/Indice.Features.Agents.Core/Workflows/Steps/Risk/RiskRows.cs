using System.Globalization;
using System.Text.Json;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>Reads the cells of a result row from the risk MCP server.</summary>
internal static class RiskRows
{
    /// <summary>The cell as text, or <c>null</c> when it is missing or SQL NULL. Numbers and booleans are written as JSON writes them.</summary>
    public static string? Text(this IReadOnlyDictionary<string, JsonElement> row, string column) {
        if (!row.TryGetValue(column, out var cell)) {
            return null;
        }
        return cell.ValueKind switch {
            JsonValueKind.String => cell.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => cell.ToString(),
            _ => null
        };
    }

    /// <summary>The cell as a UTC instant, or <c>null</c> when it is missing or not a date-time.</summary>
    public static DateTimeOffset? Time(this IReadOnlyDictionary<string, JsonElement> row, string column) =>
        DateTimeOffset.TryParse(row.Text(column), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value) ? value : null;

    /// <summary>The cell as a decimal, or <c>null</c>.</summary>
    public static decimal? Number(this IReadOnlyDictionary<string, JsonElement> row, string column) =>
        decimal.TryParse(row.Text(column), NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>The cell as a boolean (a JSON boolean, or a bit column), or <c>null</c>.</summary>
    public static bool? Flag(this IReadOnlyDictionary<string, JsonElement> row, string column) => row.Text(column)?.ToLowerInvariant() switch {
        "true" or "1" => true,
        "false" or "0" => false,
        _ => null
    };

    /// <summary>The cell parsed as JSON: the log tables keep their payload in <c>nvarchar(max)</c> JSON columns. <c>null</c> when it is missing or not JSON.</summary>
    public static JsonElement? Json(this IReadOnlyDictionary<string, JsonElement> row, string column) {
        var text = row.Text(column);
        if (string.IsNullOrWhiteSpace(text)) {
            return null;
        }
        try {
            return JsonDocument.Parse(text).RootElement.Clone();
        } catch (JsonException) {
            return null;
        }
    }

    /// <summary>A property of a JSON object, matched case-insensitively (the stores use both camelCase and PascalCase).</summary>
    public static JsonElement? Property(this JsonElement? element, string name) {
        if (element is not { ValueKind: JsonValueKind.Object } obj) {
            return null;
        }
        foreach (var property in obj.EnumerateObject()) {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)) {
                return property.Value;
            }
        }
        return null;
    }

    /// <summary>A JSON value as text, or <c>null</c> for null, objects and arrays.</summary>
    public static string? AsText(this JsonElement? element) => element is { } value
        ? value.ValueKind switch {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.ToString(),
            _ => null
        }
        : null;
}
