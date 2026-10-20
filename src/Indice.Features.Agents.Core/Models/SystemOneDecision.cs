using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Models;

/// <summary>The answers of a system-one decision, returned by <see cref="Services.ISystemOneClient.DecideAsync"/>.</summary>
public class SystemOneDecision
{
    /// <summary>The answers, keyed by question name. A question the model did not answer is missing.</summary>
    public IReadOnlyDictionary<string, SystemOneAnswer> Answers { get; init; } = new Dictionary<string, SystemOneAnswer>();

    /// <summary>Token usage of the decision call.</summary>
    public UsageDetails? Usage { get; init; }

    /// <summary>Returns the answer to the question <paramref name="name"/>, or <c>null</c> when it was not asked or not answered.</summary>
    /// <param name="name">The question name.</param>
    public SystemOneAnswer? Get(string name) => Answers.TryGetValue(name, out var answer) ? answer : null;

}
