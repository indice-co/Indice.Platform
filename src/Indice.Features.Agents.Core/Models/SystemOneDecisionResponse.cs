using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models;

/// <summary>The body of a system-one decision response, as the endpoint returns it; mapped to a <see cref="SystemOneDecision"/>.</summary>
internal sealed class SystemOneDecisionResponse
{
    /// <summary>The deployed model that answered.</summary>
    public string? Model { get; init; }

    /// <summary>The answers, keyed by question name.</summary>
    public Dictionary<string, SystemOneAnswerPayload>? Answers { get; init; }

    /// <summary>The token usage.</summary>
    public SystemOneUsagePayload? Usage { get; init; }
}

/// <summary>One answer of a <see cref="SystemOneDecisionResponse"/>.</summary>
internal sealed class SystemOneAnswerPayload
{
    /// <summary>The question kind, <c>noul</c> or <c>choice</c>.</summary>
    public string? Type { get; init; }

    /// <summary>For a noul: the probability (0..1) that the condition holds.</summary>
    public double? Noul { get; init; }

    /// <summary>For a choice: the chosen option.</summary>
    public string? Choice { get; init; }

    /// <summary>For a choice: the probability of each option.</summary>
    public Dictionary<string, double>? Probabilities { get; init; }

    /// <summary>For a choice: the probability of the chosen option.</summary>
    public double? Confidence { get; init; }
}

/// <summary>The token usage of a <see cref="SystemOneDecisionResponse"/>.</summary>
internal sealed class SystemOneUsagePayload
{
    /// <summary>The input tokens.</summary>
    [JsonPropertyName("input_tokens")]
    public long? InputTokens { get; init; }

    /// <summary>The output tokens.</summary>
    [JsonPropertyName("output_tokens")]
    public long? OutputTokens { get; init; }
}
