using System.Text.Json;
using System.Text.Json.Serialization;
using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Payments;
using Indice.Security;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Handles the chosen payment method and produces the terminal response. When Stripe is selected, the case's
/// payment attempt is created (or reused) and the inline Stripe payment form is sent to the chat; for any other
/// method the user is simply thanked.
/// </summary>
public class PaymentCompletedStep : Executor<PaymentRequestPort.PaymentResponse, OperationState>
{
    private const string StripeMethod = "Stripe";
    private static readonly JsonSerializerOptions PayloadJsonOptions = new(JsonSerializerDefaults.Web) {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AgentsClaimsPrincipalSelector _claimsPrincipalSelector;
    private readonly ILogger<PaymentCompletedStep> _logger;

    /// <summary>Creates a new <see cref="PaymentCompletedStep"/>.</summary>
    public PaymentCompletedStep(
        AgentMessageLocalizer messageLocalizer,
        IServiceScopeFactory scopeFactory,
        AgentsClaimsPrincipalSelector claimsPrincipalSelector,
        ILogger<PaymentCompletedStep> logger) : base(nameof(PaymentCompletedStep)) {
        _messageLocalizer = messageLocalizer;
        _scopeFactory = scopeFactory;
        _claimsPrincipalSelector = claimsPrincipalSelector;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async ValueTask<OperationState> HandleAsync(
        PaymentRequestPort.PaymentResponse response,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(response);
        if (string.Equals(response.Method?.Trim(), StripeMethod, StringComparison.OrdinalIgnoreCase)) {
            await SendStripePaymentAsync(context, cancellationToken);
            return OperationState.End;
        }
        await context.Say(Id, _messageLocalizer.PaymentCompletedMessage, cancellationToken);
        return OperationState.End;
    }

    // The case is the payment key (paid once per case). The attempt and the publishable key are resolved here so the
    // UI renders the card form straight from the chat part, without extra round-trips.
    private async Task SendStripePaymentAsync(IWorkflowContext context, CancellationToken cancellationToken) {
        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var conversation = await context.GetConversationStateAsync(cancellationToken);
        var caseId = caseData.ReferenceId;
        using var scope = _scopeFactory.CreateScope();
        var payments = scope.ServiceProvider.GetService<IPaymentsService>();
        if (payments is null || string.IsNullOrWhiteSpace(caseId)) {
            _logger.LogWarning("Stripe payment requested but payments are not enabled or the case reference is missing.");
            await context.Say(Id, _messageLocalizer.PaymentCompletedMessage, cancellationToken);
            return;
        }
        if (caseData.PaymentAmount is not > 0m) {
            _logger.LogWarning("Stripe payment requested for case {CaseId} but the payment amount is missing or not positive ({Amount}).", caseId, caseData.PaymentAmount);
            await context.Say(Id, _messageLocalizer.PaymentCompletedMessage, cancellationToken);
            return;
        }
        var userId = _claimsPrincipalSelector()?.FindSubjectId() ?? conversation.ConversationId;
        Guid conversationId = Guid.Parse(conversation.ConversationId);
        var session = await payments.CreateOrReuseAttemptAsync(userId, caseId, AgentsConstants.DefaultTenantId, caseData.PaymentAmount.Value, conversationId, cancellationToken);
        var publicKey = payments.GetPublicKey();
        var payload = new StripePaymentPayload(
            Method: StripeMethod,
            CaseId: caseId,
            PublishableKey: publicKey.PublishableKey,
            Session: session,
            PaymentMethodOrder: publicKey.PaymentMethodOrder);
        var stripePart = new DataContent(JsonSerializer.SerializeToUtf8Bytes(payload, PayloadJsonOptions), AgentsConstants.MediaTypes.Payment.Stripe) {
            Name = "StripePayment"
        };
        _logger.LogDebug("Stripe selected; sending the inline Stripe payment form for case {CaseId}.", caseId);
        await context.Say(Id, stripePart, cancellationToken);
    }

    /// <summary>Payload of the <see cref="AgentsConstants.MediaTypes.Payment.Stripe"/> chat part.</summary>
    /// <param name="Method">The selected payment method.</param>
    /// <param name="CaseId">The case being paid.</param>
    /// <param name="PublishableKey">The Stripe publishable key.</param>
    /// <param name="Session">The case's payment attempt (with the client secret while it is confirmable).</param>
    /// <param name="PaymentMethodOrder">Optional display order of the payment methods.</param>
    public record StripePaymentPayload(string Method, string CaseId, string? PublishableKey, PaymentSessionModel? Session, string[]? PaymentMethodOrder = null);
}
