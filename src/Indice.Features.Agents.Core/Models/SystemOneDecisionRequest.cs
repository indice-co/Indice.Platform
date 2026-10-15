namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// What a caller asks the system-one decision model: the latest message, the conversation it belongs to and the
/// typed questions to answer. The service builds the actual request (history, context, payload) from it.
/// </summary>
public class SystemOneDecisionRequest
{
    /// <summary>The conversation whose recent history is sent as decision state, or <c>null</c> for none.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>The latest user message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Name of the prompt template (e.g. <c>IntentRouter.SystemOne</c>) whose text is sent as context — what the
    /// assistant covers. Optional; a missing template sends no context.
    /// </summary>
    public string? ContextPrompt { get; init; }

    /// <summary>The questions to answer, keyed by the name the answers come back under.</summary>
    public Dictionary<string, SystemOneQuestion> Questions { get; init; } = [];
}
