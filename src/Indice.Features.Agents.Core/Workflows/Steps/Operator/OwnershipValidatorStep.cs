using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Options;

namespace Indice.Features.Agents.Core.Workflows.Steps.Operator;

/// <summary>
/// Step 3 of the Cases workflow: Validates user's ownership confirmation input.
/// Receives the user's reply from the ownership request port and
/// compares it with the actual case data field. Supports up to <see cref="AgentsOptions.CaseWorkflowOptions.MaxOwnershipValidationAttempts"/> validation attempts.
/// </summary>
public sealed class OwnershipValidatorStep : Executor<OwnershipConfirmationResponse, UserInputValidationOutput>
{
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

        var attempt = confirmation.VerificationData.Attempt + 1;

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
/// <summary>
/// Response payload delivered to the Cases workflow when the user replies to the ownership verification request.
/// Produced by the host (chat client) as an external response to the ownership confirmation request port.
/// </summary>
/// <param name="VerificationData">The ownership verification data originally emitted by the OwnershipVerifier step.</param>
/// <param name="UserInput">The raw text the user submitted to confirm ownership of the case.</param>
/// <param name="Attempt">The current ownership confirmation attempt number.</param>
public record OwnershipConfirmationResponse(
    OwnershipVerificationOutput VerificationData,
    string UserInput,
    int Attempt = 0);

/// <summary>
/// Output of the UserInputValidator step with validation result and retry tracking.
/// </summary>
/// <param name="OwnershipVerificationData">The original ownership verification output.</param>
/// <param name="IsValid">Whether the user's input matches the case data field.</param>
/// <param name="ErrorMessage">Error message if validation failed; null if valid.</param>
/// <param name="ValidationAttempt">Current attempt number (1 or 2; max 2 attempts allowed).</param>
/// <param name="UserInput">The user's input to verify (stored for comparison).</param>
public record UserInputValidationOutput(
    OwnershipVerificationOutput OwnershipVerificationData,
    bool IsValid,
    string? ErrorMessage,
    int ValidationAttempt,
    string UserInput);
