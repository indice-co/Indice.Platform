using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Ports;

/// <summary>
/// Represents a request port for choosing a payment method in the workflow.
/// </summary>
public static class PaymentRequestPort
{
    /// <summary>
    /// Represents a request for the user to choose a payment method.
    /// </summary>
    /// <param name="Methods">The payment methods the user can choose from.</param>
    public record PaymentRequest(IReadOnlyList<string> Methods);

    /// <summary>
    /// Represents a response containing the payment method the user chose.
    /// </summary>
    /// <param name="Method">The chosen payment method.</param>
    public record PaymentResponse(string Method);

    /// <summary>
    /// Creates a request port for choosing a payment method in the workflow.
    /// </summary>
    /// <param name="id">The identifier for the request port.</param>
    /// <returns>A request port for choosing a payment method.</returns>
    public static RequestPort<PaymentRequest, PaymentResponse> Create(string id = nameof(PaymentRequest)) => RequestPort.Create<PaymentRequest, PaymentResponse>(id);
}
