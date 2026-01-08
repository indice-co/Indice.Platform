using Indice.Features.Cases.Workflows.Models.Decision;

namespace Indice.Features.Cases.Workflows.Extensions;

public static class DecisionExtensions
{
    public static string BuildExpression(this List<RuleCondition> conditions) {
        var exprParts = new List<string>();

        foreach (var condition in conditions) {
            switch (condition.FieldType) {
                case FieldType.String:
                    if (string.IsNullOrEmpty(condition.Value?.ToString() ?? string.Empty)) {
                        break;
                    }

                    exprParts.Add($"{condition.Field} {condition.Operator} \"{condition.Value}\"");
                    break;

                case FieldType.Bool:
                    if (condition.Value == null) {
                        break;
                    }

                    exprParts.Add($"{condition.Field} {condition.Operator} {condition.Value.ToString()!.ToLower()}");
                    break;

                case FieldType.Date:
                    exprParts.Add(BuildDateExpression(condition));
                    break;

                case FieldType.Int:
                    exprParts.Add(BuildIntExpression(condition));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return string.Join(" && ", exprParts.Where(e => !string.IsNullOrEmpty(e)));
    }

    private static int ParseDateToInt(string value) {
        if (!DateTime.TryParse(value, out var dt)) {
            throw new FormatException($"Invalid date value '{value}'. Expected YYYY-MM-DD.");
        }

        return dt.Year * 10000 + dt.Month * 100 + dt.Day;
    }

    private static string BuildDateExpression(RuleCondition condition) {
        var valueStr = condition.Value?.ToString()?.Trim();

        if (string.IsNullOrEmpty(valueStr))
            return null!;

        if (!valueStr.Contains("...")) {
            var dateInt = ParseDateToInt(valueStr);
            return $"{condition.Field} {condition.Operator} {dateInt}";
        }

        if (!(valueStr.StartsWith('[') || valueStr.StartsWith('(')) ||
            !(valueStr.EndsWith(']') || valueStr.EndsWith(')'))) {
            throw new FormatException(
                $"Invalid date range syntax '{valueStr}'. Use [YYYY-MM-DD...YYYY-MM-DD].");
        }

        var lowerInclusive = valueStr.StartsWith('[');
        var upperInclusive = valueStr.EndsWith(']');

        var trimmedStr = valueStr.TrimStart('[', '(').TrimEnd(']', ')');
        var parts = trimmedStr.Split("...");

        var expressions = new List<string>();

        if (!string.IsNullOrWhiteSpace(parts[0])) {
            var min = ParseDateToInt(parts[0].Trim());
            expressions.Add($"{condition.Field} {(lowerInclusive ? ">=" : ">")} {min}");
        }

        if (!string.IsNullOrWhiteSpace(parts[1])) {
            var max = ParseDateToInt(parts[1].Trim());
            expressions.Add($"{condition.Field} {(upperInclusive ? "<=" : "<")} {max}");
        }

        if (expressions.Count == 0)
            throw new FormatException($"Date range must have at least one bound: {valueStr}");

        return expressions.Count == 1
            ? expressions[0]
            : $"({string.Join(" && ", expressions)})";
    }


    private static string BuildIntExpression(RuleCondition condition) {
        var valueStr = condition.Value?.ToString()?.Trim();

        if (string.IsNullOrEmpty(valueStr)) {
            return null!;
        }

        if (!valueStr.Contains("...")) {
            return $"{condition.Field} {condition.Operator} {valueStr}";
        }

        if (!(valueStr.StartsWith('[') || valueStr.StartsWith('(')) ||
            !(valueStr.EndsWith(']') || valueStr.EndsWith(')'))) {
            throw new FormatException($"Invalid range syntax '{valueStr}'. Use [min...max], (min...max], etc.");
        }

        var lowerInclusive = valueStr.StartsWith('[');
        var upperInclusive = valueStr.EndsWith(']');

        var trimmedStr = valueStr.TrimStart('[', '(').TrimEnd(']', ')');

        var parts = trimmedStr.Split("...");

        var minPart = parts.Length > 0 ? parts[0].Trim() : null;
        var maxPart = parts.Length > 1 ? parts[1].Trim() : null;

        var expressions = new List<string>();

        if (!string.IsNullOrEmpty(minPart)) {
            if (!int.TryParse(minPart, out var min)) {
                throw new FormatException($"Invalid min in range: {valueStr}");
            }

            expressions.Add($"{condition.Field} {(lowerInclusive ? ">=" : ">")} {min}");
        }

        if (!string.IsNullOrEmpty(maxPart)) {
            if (!int.TryParse(maxPart, out var max)) {
                throw new FormatException($"Invalid max in range: {valueStr}");
            }

            expressions.Add($"{condition.Field} {(upperInclusive ? "<=" : "<")} {max}");
        }

        if (expressions.Count == 0) {
            throw new FormatException($"Range must have at least one bound: {valueStr}");
        }

        return expressions.Count == 1
            ? expressions[0]
            : $"({string.Join(" && ", expressions)})";
    }
}