using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

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

    /// <summary>Maps the response to a <see cref="SystemOneDecision"/>, keeping only answers to the asked <paramref name="questions"/>.</summary>
    /// <param name="questions">The questions of the request, keyed by name.</param>
    public SystemOneDecision ToDecision(IReadOnlyDictionary<string, SystemOneQuestion> questions) {
        var answers = new Dictionary<string, SystemOneAnswer>();
        foreach (var (name, question) in questions) {
            if (Answers?.GetValueOrDefault(name)?.ToAnswer(question) is { } answer) {
                answers[name] = answer;
            }
        }
        return new SystemOneDecision {
            Answers = answers,
            Usage = Usage is null ? null : new UsageDetails {
                InputTokenCount = Usage.InputTokens,
                OutputTokenCount = Usage.OutputTokens,
                TotalTokenCount = (Usage.InputTokens ?? 0) + (Usage.OutputTokens ?? 0),
            },
        };
    }
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

    /// <summary>Maps the payload to a <see cref="SystemOneAnswer"/> for <paramref name="question"/>, or <c>null</c> when a noul has no probability.</summary>
    /// <param name="question">The question this answers.</param>
    public SystemOneAnswer? ToAnswer(SystemOneQuestion question) {
        if (question.Type == SystemOneQuestionType.Noul) {
            return Noul is not { } probability ? null : new SystemOneAnswer {
                Type = SystemOneQuestionType.Noul,
                Probability = probability,
                Confidence = Math.Max(probability, 1 - probability),
            };
        }
        var probabilities = new Dictionary<string, double>(Probabilities ?? [], StringComparer.OrdinalIgnoreCase);
        // Map the answer back to the option exactly as the caller spelled it; anything else is not an answer.
        var chosenOption = question.Criteria?.Keys.FirstOrDefault(key => string.Equals(key, Choice, StringComparison.OrdinalIgnoreCase));
        var confidence = chosenOption is null ? 0
            : probabilities.TryGetValue(chosenOption, out var chosen) ? chosen
            : Confidence ?? 0;
        return new SystemOneAnswer {
            Type = SystemOneQuestionType.Choice,
            Choice = chosenOption,
            Probabilities = probabilities,
            Confidence = confidence,
        };
    }
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
