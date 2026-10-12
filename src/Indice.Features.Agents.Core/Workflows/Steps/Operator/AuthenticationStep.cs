using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.Ports;
using Indice.Features.Agents.Core.Workflows.State;
using Indice.Text;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Validates user's ownership confirmation input.
/// Receives the user's reply from the ownership request port and
/// compares it with the actual case data field. Supports up to <see cref="CustomerWorkflowOptions.MaxOwnershipValidationAttempts"/> validation attempts.
/// </summary>
[SendsMessage(typeof(ChallengeRequestPort.ChallengeRequest))]
[SendsMessage(typeof(OperationState))]
[YieldsOutput(typeof(OperationState))]
public sealed class AuthenticationStep : Executor<ChallengeRequestPort.ChallengeResponse>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly int _maxValidationAttempts;

    /// <summary>Creates a new <see cref="AuthenticationStep"/>.</summary>
    public AuthenticationStep(AgentMessageLocalizer messageLocalizer, IOptions<CustomerWorkflowOptions> options) : base(nameof(AuthenticationStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _maxValidationAttempts = options.Value.MaxOwnershipValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        ChallengeRequestPort.ChallengeResponse confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(confirmation);
        var userInput = confirmation.userInput;

        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var verificationData = caseData.ChallengValue!;

        // Validate the input against the actual case field value
        var isValid = CompareInputWithCaseField(userInput, verificationData);

        if (!isValid) {
            var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
            await context.SetApprovalStateAsync(attempt, cancellationToken);
            if (attempt >= _maxValidationAttempts) {
                await context.SetApprovalStateAsync(null, cancellationToken);
                await context.Say(Id, _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts));
                await context.YieldOutputAsync(OperationState.End);
                return;
            }
            await context.Say(Id, _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts));
            await context.SendMessageAsync(new ChallengeRequestPort.ChallengeRequest(_messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts)));
            return;
        }
        await context.SetApprovalStateAsync(null, cancellationToken);
        await context.Say(Id, _messageLocalizer.ChallengeSucceeded);
        await context.SendMessageAsync(OperationState.Next(nameof(OtpCodeSendStep)));
    }

    /// <summary>
    /// Compares user input with the case field value, ignoring case, accents, separators (spaces, dashes, etc.)
    /// and whether Greek or Latin characters were used.
    /// </summary>
    private static bool CompareInputWithCaseField(string userInput, string actualValue) {
        if (string.IsNullOrWhiteSpace(userInput) || string.IsNullOrWhiteSpace(actualValue))
            return false;
        var normalizedInput = NormalizeForComparison(userInput);
        var normalizedActual = NormalizeForComparison(actualValue);
        return normalizedInput.Length > 0 && string.Equals(normalizedInput, normalizedActual, StringComparison.Ordinal);
    }

    /// <summary>Upper-cases, strips accents and non alphanumeric characters, and transliterates Greek to ASCII.</summary>
    private static string NormalizeForComparison(string value) {
        var decomposed = value.ToUpperInvariant();
        return Greeklish.Translate(decomposed);
    }
}

