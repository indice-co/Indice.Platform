using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Events;

/// <summary>Progress reporting for pipeline steps.</summary>
public static class StepProgressWorkflowContextExtensions
{
    /// <summary>
    /// Emits an ephemeral <see cref="StepProgressContent"/> update for the step identified by <paramref name="executorId"/>.
    /// Rides the same <see cref="AgentResponseUpdateEvent"/> channel as streamed text, so it survives when the workflow is hosted
    /// as an agent (where executor lifecycle events are not forwarded), and is stripped before the turn is persisted.
    /// </summary>
    public static ValueTask EmitProgressAsync(this IWorkflowContext context, string executorId, string label, CancellationToken cancellationToken = default)
        => context.AddEventAsync(new AgentResponseUpdateEvent(executorId, new AgentResponseUpdate(ChatRole.Assistant, [new StepProgressContent(label)])), cancellationToken);
}
