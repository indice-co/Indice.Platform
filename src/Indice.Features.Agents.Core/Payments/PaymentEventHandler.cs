namespace Indice.Features.Agents.Core.Payments;

/// <summary>Hook for the business flow that runs once a payment is authorized (or fails/cancels). Called from the webhook.</summary>
public interface IPaymentEventHandler
{
    /// <summary>Called when the status of a payment attempt changes.</summary>
    Task OnStatusChangedAsync(PaymentSessionModel session, CancellationToken cancellationToken = default);
}

/// <summary>Default implementation of <see cref="IPaymentEventHandler"/> that does nothing.</summary>
public class NoOpPaymentEventHandler : IPaymentEventHandler
{
    /// <inheritdoc/>
    public Task OnStatusChangedAsync(PaymentSessionModel session, CancellationToken cancellationToken = default) => Task.CompletedTask;
}