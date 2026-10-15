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

    /// <summary>
    /// When true, the whole decision is discarded (and the caller falls back to its LLM path) if this answer is
    /// missing or below the confidence threshold. Non-required answers are returned with <see cref="SystemOneAnswer.IsConfident"/> false instead.
    /// </summary>
    public bool Required { get; init; }

    /// <summary>Per-question override of <c>Dex:SystemOne:MinConfidence</c>.</summary>
    public double? MinConfidence { get; init; }

    /// <summary>Creates a yes/no question.</summary>
    /// <param name="instructions">The question.</param>
    /// <param name="required">Whether an unsure answer discards the whole decision.</param>
    public static SystemOneQuestion Noul(string instructions, bool required = false) =>
        new() { Type = SystemOneQuestionType.Noul, Instructions = instructions, Required = required };

    /// <summary>Creates a single-choice question.</summary>
    /// <param name="instructions">The question.</param>
    /// <param name="options">The options and what each one means.</param>
    /// <param name="required">Whether an unsure answer discards the whole decision.</param>
    public static SystemOneQuestion Choice(string instructions, IDictionary<string, string> options, bool required = false) =>
        new() { Type = SystemOneQuestionType.Choice, Instructions = instructions, Criteria = options, Required = required };
}
