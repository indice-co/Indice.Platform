using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the question is out-of-scope.
/// Projects the classifier's <see cref="Intent.OutOfScopeReason"/> into a final
/// <see cref="RagPipelineOutput"/> envelope with no citations.
/// </summary>
public sealed class OutOfScopeResponder : Executor<PipelineStepContext<IntentOutput>, PipelineStepContext<RagPipelineOutput>>
{
    /// <summary>Creates a new <see cref="OutOfScopeResponder"/>.</summary>
    public OutOfScopeResponder() : base("OutOfScopeResponder") { }

    /// <inheritdoc/>
    public override ValueTask<PipelineStepContext<RagPipelineOutput>> HandleAsync(
        PipelineStepContext<IntentOutput> envelope,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var reason = envelope.Payload.Intent.OutOfScopeReason ?? "Sorry, that question is outside the scope of what I can answer here.";
        return ValueTask.FromResult(envelope.Next(new RagPipelineOutput {
            Answer = reason,
            Citations = Array.Empty<Citation>(),
        }));
    }
}
