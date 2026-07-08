using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows;

/// <summary>The input to <see cref="Abstractions.IDexRunner.RunAsync"/>.</summary>
public class RagRequest
{
    /// <summary>The end-user question being asked.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>Optional conversation history (oldest-first) providing context for multi-turn interactions.</summary>
    public IReadOnlyList<ChatMessage>? History { get; init; }

    /// <summary>Timestamp of when the request was created.</summary>
    public DateTimeOffset TimeStamp { get; init; } = DateTimeOffset.UtcNow;
}
