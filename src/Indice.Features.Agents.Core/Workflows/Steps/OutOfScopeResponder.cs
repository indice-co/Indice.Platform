using Indice.Features.Agents.Core.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps;

/// <summary>
/// Terminal branch of the pipeline when <c>IntentClassifier</c> decides the question is out-of-scope.
/// Projects the classifier's <see cref="Intent.OutOfScopeReason"/> into a final
/// <see cref="GroundedAnswerOutput"/> envelope with no citations.
/// </summary>
public sealed class OutOfScopeResponder : Executor<IntentOutput, GroundedAnswerOutput>
{
    /// <summary>Creates a new <see cref="OutOfScopeResponder"/>.</summary>
    public OutOfScopeResponder() : base("OutOfScopeResponder") { }

    /// <inheritdoc/>
    public override async ValueTask<GroundedAnswerOutput> HandleAsync(
        IntentOutput intentResult,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        var reason = intentResult.Intent.OutOfScopeReason ?? "Sorry, that question is outside the scope of what I can answer here.";
        await context.AddEventAsync(new AgentResponseUpdateEvent(Id, 
                                        new AgentResponseUpdate(ChatRole.Assistant, reason)
                                    ), cancellationToken);
        return new GroundedAnswerOutput(reason, [], []);
    }
}
