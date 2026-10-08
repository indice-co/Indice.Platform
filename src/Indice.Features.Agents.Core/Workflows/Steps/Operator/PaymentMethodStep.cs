using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Asks the user to choose a payment method once the case data has been presented.
/// The workflow pauses after this step and waits for the user's choice on a request port.
/// </summary>
public sealed class PaymentMethodStep : Executor<OperationState, PaymentRequestPort.PaymentRequest>
{
    private readonly IReadOnlyList<string> _paymentMethods;
    private readonly ILogger<PaymentMethodStep> _logger;

    /// <summary>Creates a new <see cref="PaymentMethodStep"/>.</summary>
    public PaymentMethodStep(IOptions<CustomerWorkflowOptions> customerWorkflowOptions, ILogger<PaymentMethodStep> logger) : base(nameof(PaymentMethodStep)) {
        _logger = logger;
        _paymentMethods = [.. customerWorkflowOptions.Value.PaymentMethods];
    }

    /// <inheritdoc/>
    public override ValueTask<PaymentRequestPort.PaymentRequest> HandleAsync(
        OperationState message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(message);
        if (_paymentMethods.Count == 0) {
            _logger.LogWarning("No payment methods configured in {Options}; the user will have no options to choose from.", nameof(CustomerWorkflowOptions));
        }
        return ValueTask.FromResult(new PaymentRequestPort.PaymentRequest(_paymentMethods));
    }
}
