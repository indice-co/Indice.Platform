using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Thanks the user once a payment method has been chosen and produces the terminal response.
/// No payment is taken: the step is a demonstration of the payment round-trip.
/// </summary>
public sealed class PaymentCompletedStep : Executor<PaymentRequestPort.PaymentResponse, OperationState>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly ILogger<PaymentCompletedStep> _logger;

    /// <summary>Creates a new <see cref="PaymentCompletedStep"/>.</summary>
    public PaymentCompletedStep(AgentMessageLocalizer messageLocalizer, ILogger<PaymentCompletedStep> logger) : base(nameof(PaymentCompletedStep)) {
        _messageLocalizer = messageLocalizer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        PaymentRequestPort.PaymentResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);
        _logger.LogInformation("Payment method selected; completing operator workflow.");
        await context.Say(Id, _messageLocalizer.PaymentCompletedMessage, cancellationToken);
        return OperationState.End;
    }
}
