using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Thanks the user once a payment method has been chosen and produces the terminal response.
/// No payment is taken: the step is a demonstration of the payment round-trip.
/// </summary>
public sealed class PaymentCompletedStep : Executor<PaymentRequestPort.PaymentResponse, OperationState>
{
    private readonly AgentMessageLocalizer _messageLocalizer;

    /// <summary>Creates a new <see cref="PaymentCompletedStep"/>.</summary>
    public PaymentCompletedStep(AgentMessageLocalizer messageLocalizer) : base(nameof(PaymentCompletedStep)) {
        _messageLocalizer = messageLocalizer;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        PaymentRequestPort.PaymentResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);
        await context.Say(Id, _messageLocalizer.PaymentCompletedMessage, cancellationToken);
        return OperationState.End;
    }
}
