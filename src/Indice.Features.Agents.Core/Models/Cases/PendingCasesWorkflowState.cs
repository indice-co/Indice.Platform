namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Pending state persisted (e.g., in a distributed cache) for a Cases workflow run that has been
/// halted awaiting user input. Contains everything needed to resume the checkpointed run.
/// </summary>
/// <param name="CheckpointJson">The serialized <c>CheckpointInfo</c> of the last completed super-step.</param>
public record PendingCasesWorkflowState(string CheckpointJson);
