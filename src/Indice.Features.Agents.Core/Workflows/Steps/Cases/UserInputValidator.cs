using Indice.Features.Agents.Core.Models.Cases;
using Indice.Features.Agents.Core.Workflows.State;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 3 of the Cases workflow: Validates user's ownership confirmation input.
/// Receives the user's reply through the ownership confirmation request port (external input) and
/// compares it with the actual case data field. Supports up to 2 validation attempts.
/// </summary>
public sealed class UserInputValidator : Executor<OwnershipConfirmationResponse, UserInputValidationOutput>
{
    private const int MaxValidationAttempts = 2;
    private const string AttemptStateKey = "OwnershipValidationAttempt";

    /// <summary>Creates a new <see cref="UserInputValidator"/>.</summary>
    public UserInputValidator() : base(nameof(UserInputValidator))
    {
    }

    /// <inheritdoc/>
    public override async ValueTask<UserInputValidationOutput> HandleAsync(
        OwnershipConfirmationResponse confirmation,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (confirmation is null)
            throw new ArgumentNullException(nameof(confirmation));

        var verificationData = confirmation.VerificationData;
        var userInput = confirmation.UserInput ?? string.Empty;

        // Track validation attempts in workflow state to support retries across resumes.
        var previousAttempts = await context.ReadStateAsync<int?>(AttemptStateKey, scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken) ?? 0;
        var attempt = previousAttempts + 1;
        await context.QueueStateUpdateAsync<int?>(AttemptStateKey, attempt, scopeName: IWorkflowContextStateExtensions.ConversationScope, cancellationToken: cancellationToken);

        // Validate the input against the actual case field value
        var isValid = CompareInputWithCaseField(
            userInput,
            verificationData.VerificationFieldValue,
            verificationData.VerificationFieldName);

        string? errorMessage = null;
        if (!isValid)
        {
            if (attempt >= MaxValidationAttempts)
            {
                errorMessage = $"Verification failed. Maximum {MaxValidationAttempts} attempts reached. Please try again later.";
            }
            else
            {
                errorMessage = $"The information provided does not match our records. Attempt {attempt} of {MaxValidationAttempts}. Please try again.";
            }
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
    private static bool CompareInputWithCaseField(string userInput, string actualValue, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return false;

        // Normalize inputs for comparison
        var normalizedInput = NormalizeFieldValue(userInput, fieldName);
        var normalizedActual = NormalizeFieldValue(actualValue, fieldName);

        return string.Equals(normalizedInput, normalizedActual, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Normalizes field values by removing common formatting characters for robust comparison.
    /// </summary>
    private static string NormalizeFieldValue(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return fieldName.ToLower() switch
        {
            "email" => value.Trim().ToLowerInvariant(),
            "phonenumber" or "phone" => System.Text.RegularExpressions.Regex.Replace(value, @"\D", ""),
            "ssn" or "socialsecuritynumber" => System.Text.RegularExpressions.Regex.Replace(value, @"\D", ""),
            "cardnumber" or "creditcard" => System.Text.RegularExpressions.Regex.Replace(value, @"\D", ""),
            _ => value.Trim()
        };
    }
}
