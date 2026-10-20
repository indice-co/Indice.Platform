namespace Indice.Features.Agents.Core.Models;

/// <summary>One typed question of a <see cref="SystemOneDecisionRequest"/>.</summary>
public class SystemOneQuestion
{
    /// <summary>The kind of decision asked for.</summary>
    public SystemOneQuestionType Type { get; init; }

    /// <summary>The question itself, e.g. <c>Which agent should handle the latest message?</c>.</summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>
    /// For <see cref="SystemOneQuestionType.Choice"/>: the options (key) and what each one means (value). For
    /// <see cref="SystemOneQuestionType.Noul"/>: optionally the <c>true</c> / <c>false</c> conditions.
    /// </summary>
    public IDictionary<string, string>? Criteria { get; init; }

    /// <summary>Creates a yes/no question.</summary>
    /// <param name="instructions">The question.</param>
    /// <param name="whenTrue">Optionally, when the answer is <c>true</c>.</param>
    /// <param name="whenFalse">Optionally, when the answer is <c>false</c>.</param>
    public static SystemOneQuestion Noul(string instructions, string? whenTrue = null, string? whenFalse = null) {
        var criteria = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(whenTrue)) {
            criteria["true"] = whenTrue;
        }
        if (!string.IsNullOrWhiteSpace(whenFalse)) {
            criteria["false"] = whenFalse;
        }
        return new() { Type = SystemOneQuestionType.Noul, Instructions = instructions, Criteria = criteria.Count > 0 ? criteria : null };
    }

    /// <summary>Creates a single-choice question.</summary>
    /// <param name="instructions">The question.</param>
    /// <param name="options">The options and what each one means.</param>
    public static SystemOneQuestion Choice(string instructions, IDictionary<string, string> options) =>
        new() { Type = SystemOneQuestionType.Choice, Instructions = instructions, Criteria = options };
}
