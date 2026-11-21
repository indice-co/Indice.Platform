using Indice.Features.Agents.Core.Extensions;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Risk;

/// <summary>
/// Terminal branch for a request that matches a report type the agent does not produce yet. It says so, and names the one it can
/// produce, instead of making a report of the wrong kind.
/// </summary>
public sealed class RiskUnsupportedResponder : Executor<RiskUnsupportedRequest, string>
{
    /// <summary>Creates a new <see cref="RiskUnsupportedResponder"/>.</summary>
    public RiskUnsupportedResponder() : base(nameof(RiskUnsupportedResponder)) { }

    /// <inheritdoc/>
    public override async ValueTask<string> HandleAsync(RiskUnsupportedRequest request, IWorkflowContext context, CancellationToken cancellationToken = default) {
        var reply = RiskMessages.For(request.Language).Unsupported(request.PlaybookId);
        await context.Say(Id, reply, cancellationToken);
        return reply;
    }
}
