namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// What an <see cref="Services.ISystemOneClient"/> is asked about a conversation: the typed questions and the optional
/// context. The counterpart of <c>ChatOptions</c> for a system-one decision.
/// </summary>
public class SystemOneDecisionOptions
{
    /// <summary>Background the model judges the conversation against, e.g. what the assistant covers. Sent as <c>state.context</c>; optional.</summary>
    public string? Instructions { get; init; }

    /// <summary>The questions to answer, keyed by the name the answers come back under.</summary>
    public Dictionary<string, SystemOneQuestion> Questions { get; init; } = [];
}
