using Indice.Features.Agents.Core.Extensions;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 3 of the Cases workflow: Validates user's ownership confirmation input.
/// Receives the user's reply from the ownership request port and
/// compares it with the actual case data field. Supports up to <see cref="AgentsOptions.CaseWorkflowOptions.MaxOwnershipValidationAttempts"/> validation attempts.
/// </summary>
[SendsMessage(typeof(ChatMessage))]
[YieldsOutput(typeof(ValidationFailureOutput))]
public sealed class OwnershipValidatorStep : Executor<ChatMessage>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly int _maxValidationAttempts;

    /// <summary>Creates a new <see cref="OwnershipValidatorStep"/>.</summary>
    public OwnershipValidatorStep(AgentMessageLocalizer messageLocalizer, IOptions<AgentsOptions> options) : base(nameof(OwnershipValidatorStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOwnershipValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask HandleAsync(
        ChatMessage confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(confirmation);
        var userInput = confirmation.Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var verificationData = caseData.VerificationValue!;

        // Validate the input against the actual case field value
        var isValid = CompareInputWithCaseField(userInput, verificationData);

        if (!isValid) {
            var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
            await context.SetApprovalStateAsync(attempt, cancellationToken);
            
            if (attempt >= _maxValidationAttempts) {
                //await context.AddEventAsync(new AgentResponseUpdateEvent(Id, new AgentResponseUpdate(ChatRole.Assistant, [new TextContent(failureMessage)])),cancellationToken);
                var attempts = await context.GetApprovalStateAsync(cancellationToken);
                await context.Say(Id, _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts));
                await context.SendMessageAsync(new ValidationFailureOutput(
                    ErrorMessage: _messageLocalizer.OwnershipVerificationFailedMaxAttemptsMessage(_maxValidationAttempts),
                    FailureStep: "OwnershipVerification",
                    AttemptsExhausted: attempts));
            }

            await context.Say(Id, _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts));
            await context.SendMessageAsync(new ChatMessage(ChatRole.Assistant, [
            new TextContent(_messageLocalizer.OwnershipVerificationMessagePrompt),
            new ErrorContent(_messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts))]));

            await context.SendMessageAsync(new ValidationFailureOutput(
                ErrorMessage: _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts),
                FailureStep: "OwnershipVerification",
                AttemptsExhausted: attempt));
        }
        await context.Say(Id, _messageLocalizer.OtvpVerificationSuccessMessage);
        await context.SendMessageAsync(new ChatMessage(ChatRole.Assistant, [
            new TextContent(_messageLocalizer.OtvpVerificationSuccessMessage)])); 
    }

    /// <summary>
    /// Compares user input with the case field value, handling various field types.
    /// </summary>
    private static bool CompareInputWithCaseField(string userInput, string actualValue) {
        if (string.IsNullOrWhiteSpace(userInput))
            return false;
        // Normalize inputs for comparison
        var normalizedInput = userInput.Trim();
        var normalizedActual = actualValue.Trim();
        return string.Equals(normalizedInput, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Output when validation fails after maximum retry attempts have been exhausted.
/// This terminal output is used for workflow routing when retries are exceeded.
/// </summary>
/// <param name="ErrorMessage">The error message to display.</param>
/// <param name="FailureStep">The step where validation failed (e.g., "OwnershipVerification", "OtpValidation").</param>
/// <param name="AttemptsExhausted">Number of attempts made before giving up.</param>
public record ValidationFailureOutput(
    string ErrorMessage,
    string FailureStep,
    int AttemptsExhausted);
