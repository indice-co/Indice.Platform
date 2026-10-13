using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Presents selected case data after OTP verification completes.
/// </summary>
public sealed class DataPresenterStep : Executor<OperationState, OperationState>
{
    private readonly ICustomerDataCardRenderer _presentationFormatter;
    private readonly ILogger<DataPresenterStep> _logger;

    /// <summary>Creates a new <see cref="DataPresenterStep"/>.</summary>
    public DataPresenterStep(ICustomerDataCardRenderer presentationFormatter, ILogger<DataPresenterStep> logger) : base(nameof(DataPresenterStep)) {
        _presentationFormatter = presentationFormatter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        OperationState message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        var state = await context.GetOperatorStateAsync(cancellationToken);
        AIContent presentation;
        try {
            presentation = _presentationFormatter.Render(state);
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to render customer data card for case {ReferenceId} ({DataType}).", state.ReferenceId, state.DataType);
            throw;
        }
        await context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [presentation])), cancellationToken);
        return OperationState.End;
    }
}
