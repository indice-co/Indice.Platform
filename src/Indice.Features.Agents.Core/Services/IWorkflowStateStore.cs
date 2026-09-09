namespace Indice.Features.Agents.Core.Services;

/// <summary>Persistence boundary for per-conversation workflow runtime state.</summary>
public interface IWorkflowStateStore
{
    /// <summary>Reads the raw workflow state JSON for a conversation.</summary>
    Task<string?> GetStateJsonAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>Writes the raw workflow state JSON for a conversation.</summary>
    Task SetStateJsonAsync(Guid conversationId, string? stateJson, CancellationToken cancellationToken = default);

    /// <summary>Clears workflow state for a conversation.</summary>
    Task ClearStateAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
