namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>
/// Pending state persisted (e.g., in a distributed cache) for a Cases workflow run that has been
/// halted awaiting user input. Contains everything needed to resume the checkpointed run.
/// </summary>
/// <param name="CheckpointJson">The serialized <c>CheckpointInfo</c> of the last completed super-step.</param>
/// <param name="PortId">The request-port id that is waiting for a response.</param>
/// <param name="RequestId">The pending external request id that must be answered on resume.</param>
/// <param name="RequestPayloadJson">The serialized request payload required to build the typed response object.</param>
/// <param name="WorkflowName">The workflow key this checkpoint belongs to.</param>
/// <param name="CreatedAt">When the pending request was persisted.</param>
public record PendingCasesWorkflowState(
    string CheckpointJson,
    string PortId,
    string RequestId,
    string RequestPayloadJson,
    string WorkflowName,
    DateTimeOffset CreatedAt);
