using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// A system-one decision in which every required question was answered confidently. Returned by
/// <c>SystemOneDecisionService.DecideAsync</c>; a <c>null</c> result there means "fall back to the LLM".
/// </summary>
public class SystemOneDecision
{
    /// <summary>The answers, keyed by question name.</summary>
    public IReadOnlyDictionary<string, SystemOneAnswer> Answers { get; init; } = new Dictionary<string, SystemOneAnswer>();

    /// <summary>Token usage of the decision call.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>Returns the answer to the question <paramref name="name"/>, or <c>null</c> when it was not asked or not answered.</summary>
    /// <param name="name">The question name.</param>
    public SystemOneAnswer? Get(string name) => Answers.TryGetValue(name, out var answer) ? answer : null;
}
