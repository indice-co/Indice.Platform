using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Asks the user to choose a payment method once the case data has been presented.
/// The workflow pauses after this step and waits for the user's choice on a request port.
/// </summary>
public sealed class PaymentMethodStep : Executor<OperationState, PaymentRequestPort.PaymentRequest>
{
    private readonly IReadOnlyList<string> _paymentMethods;

    /// <summary>Creates a new <see cref="PaymentMethodStep"/>.</summary>
    public PaymentMethodStep(IOptions<CustomerWorkflowOptions> customerWorkflowOptions) : base(nameof(PaymentMethodStep)) {
        _paymentMethods = [.. customerWorkflowOptions.Value.PaymentMethods];
    }

    /// <inheritdoc/>
    public override ValueTask<PaymentRequestPort.PaymentRequest> HandleAsync(
        OperationState message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        return ValueTask.FromResult(new PaymentRequestPort.PaymentRequest(_paymentMethods));
    }
}
