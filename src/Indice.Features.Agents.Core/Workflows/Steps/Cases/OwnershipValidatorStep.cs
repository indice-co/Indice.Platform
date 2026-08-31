using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 3 of the Cases workflow: Validates user's ownership confirmation input.
/// Receives the user's reply through the ownership confirmation request port (external input) and
/// compares it with the actual case data field. Supports up to <see cref="AgentsOptions.CaseWorkflowOptions.MaxOwnershipValidationAttempts"/> validation attempts.
/// </summary>
public sealed class OwnershipValidatorStep : Executor<OwnershipConfirmationResponse, UserInputValidationOutput>
{
    private const string AttemptStateKey = "OwnershipValidationAttempt";
    private readonly AgentMessageLocalizer _messageLocalizer;
    private readonly int _maxValidationAttempts;

    /// <summary>Creates a new <see cref="OwnershipValidatorStep"/>.</summary>
    public OwnershipValidatorStep(AgentMessageLocalizer messageLocalizer, IOptions<AgentsOptions> options) : base(nameof(OwnershipValidatorStep))
    {
        _messageLocalizer = messageLocalizer ?? throw new ArgumentNullException(nameof(messageLocalizer));
        _maxValidationAttempts = options.Value.CasesWorkflow.MaxOwnershipValidationAttempts;
    }

    /// <inheritdoc/>
    public override async ValueTask<UserInputValidationOutput> HandleAsync(
        OwnershipConfirmationResponse confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(confirmation);

        var verificationData = confirmation.VerificationData;
        var userInput = confirmation.UserInput ?? string.Empty;

        // Track validation attempts in workflow state to support retries across resumes.
        var previousAttempts = await context.ReadStateAsync<int?>(AttemptStateKey, scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken) ?? 0;
        var attempt = previousAttempts + 1;
        await context.QueueStateUpdateAsync<int?>(AttemptStateKey, attempt, scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken);

        // Validate the input against the actual case field value
        var isValid = CompareInputWithCaseField(
            userInput,
            verificationData.VerificationFieldValue);

        string? errorMessage = null;
        if (!isValid)
        {
            errorMessage = attempt >= _maxValidationAttempts
                ? _messageLocalizer.VerificationFailedMaxAttempts(_maxValidationAttempts)
                : _messageLocalizer.VerificationFailedRetry(attempt, _maxValidationAttempts);
        }

        return await ValueTask.FromResult(new UserInputValidationOutput(
            OwnershipVerificationData: verificationData,
            IsValid: isValid,
            ErrorMessage: errorMessage,
            ValidationAttempt: attempt,
            UserInput: userInput));
    }

    /// <summary>
    /// Compares user input with the case field value, handling various field types.
    /// </summary>
    private static bool CompareInputWithCaseField(string userInput, string actualValue)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return false;
        // Normalize inputs for comparison
        var normalizedInput = userInput.Trim();
        var normalizedActual = actualValue.Trim();
        return string.Equals(normalizedInput, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

}
