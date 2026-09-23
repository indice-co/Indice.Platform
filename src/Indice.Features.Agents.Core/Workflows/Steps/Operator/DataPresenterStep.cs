using System.Net.Mime;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Presents selected case data after OTP verification completes.
/// </summary>
public sealed class DataPresenterStep : Executor<OperationState, OperationState>
{
    private readonly ICustomerDataCardRenderer _presentationFormatter;

    /// <summary>Creates a new <see cref="DataPresenterStep"/>.</summary>
    public DataPresenterStep(ICustomerDataCardRenderer presentationFormatter) : base(nameof(DataPresenterStep)) {
        _presentationFormatter = presentationFormatter;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        OperationState message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        var state = await context.GetOperatorStateAsync(cancellationToken);
        var presentation = _presentationFormatter.Render(state);
        await context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [presentation])), cancellationToken);
        return OperationState.End;
    }
}
