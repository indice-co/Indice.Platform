namespace Indice.Features.Agents.Core.Services;

/// <summary>
/// Durable, per-conversation storage for the state a multi-turn workflow needs in order to resume where it
/// stopped. Each HTTP turn runs the workflow from scratch, so the step the run stopped at — plus whatever the
/// steps agreed on so far — is kept here between turns, serialized as JSON.
/// </summary>
public interface IWorkflowStateStore
{
    /// <summary>Reads the state stored for <paramref name="conversationId"/>, or <c>null</c> when the conversation carries none (or holds a payload of a different shape).</summary>
    Task<TState?> LoadAsync<TState>(Guid conversationId, CancellationToken cancellationToken = default) where TState : class;

    /// <summary>Writes <paramref name="state"/> for <paramref name="conversationId"/>, replacing whatever was stored before. A <c>null</c> state clears it.</summary>
    Task SaveAsync<TState>(Guid conversationId, TState? state, CancellationToken cancellationToken = default) where TState : class;
}
