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
public sealed class OwnershipValidatorStep : Executor<ChatMessage, ChatMessage>
{
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly int _maxValidationAttempts;

    /// <summary>Creates a new <see cref="OwnershipValidatorStep"/>.</summary>
    public OwnershipValidatorStep(AgentMessageLocalizer messageLocalizer, IOptions<AgentsOptions> options) : base(nameof(OwnershipValidatorStep)) {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOwnershipValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask<ChatMessage> HandleAsync(
        ChatMessage confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(confirmation);
        var userInput = confirmation.Contents.OfType<TextContent>().FirstOrDefault()?.Text ?? string.Empty;

        var caseData = await context.GetOperatorStateAsync(cancellationToken);
        var verificationData = caseData.VerificationValue;



        // Validate the input against the actual case field value
        var isValid = CompareInputWithCaseField(
            userInput,
            verificationData);

        string? errorMessage = null;
        if (!isValid) {
            var attempt = (await context.GetApprovalStateAsync(cancellationToken)) + 1;
            await context.SetApprovalStateAsync(attempt, cancellationToken);
            errorMessage = attempt >= _maxValidationAttempts
                ? _messageLocalizer.VerificationFailedMaxAttempts(_maxValidationAttempts)
                : _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts);
        }
        return await ValueTask.FromResult(new ChatMessage(ChatRole.Assistant, [
            new TextContent(_messageLocalizer.OwnershipVerificationMessagePrompt),
            new ErrorContent(errorMessage)]));
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
/// Response payload delivered to the Cases workflow when the user replies to the ownership verification request.
/// Produced by the host (chat client) as an external response to the ownership confirmation request port.
/// </summary>
/// <param name="UserInput">The raw text the user submitted to confirm ownership of the case.</param>
/// <param name="Attempt">The current ownership confirmation attempt number.</param>
public record OwnershipConfirmationResponse(
    string UserInput,
    int Attempt = 0);