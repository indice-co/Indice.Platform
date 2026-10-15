namespace Indice.Features.Agents.Core.Models;

/// <summary>The system-one model's answer to one <see cref="SystemOneQuestion"/>.</summary>
public class SystemOneAnswer
{
    /// <summary>The question kind this answers.</summary>
    public SystemOneQuestionType Type { get; init; }

    /// <summary>For a choice: the chosen option; otherwise <c>null</c>.</summary>
    public string? Choice { get; init; }

    /// <summary>For a noul: the probability (0..1) that the condition holds; otherwise <c>null</c>.</summary>
    public double? Probability { get; init; }

    /// <summary>For a noul: whether the condition holds (<see cref="Probability"/> of at least 0.5).</summary>
    public bool IsTrue => Probability >= 0.5;

    /// <summary>For a choice: the probability of each option; otherwise empty.</summary>
    public IReadOnlyDictionary<string, double> Probabilities { get; init; } = new Dictionary<string, double>();

    /// <summary>How sure the model is: the chosen option's probability for a choice, <c>max(p, 1 - p)</c> for a noul.</summary>
    public double Confidence { get; init; }

    /// <summary>Whether <see cref="Confidence"/> reaches the threshold (and, for a choice, the option is one of the criteria).</summary>
    public bool IsConfident { get; init; }
}
