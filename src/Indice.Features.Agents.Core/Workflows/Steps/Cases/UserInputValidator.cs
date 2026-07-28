using Indice.Features.Agents.Core.Models.Cases;
using Microsoft.Agents.AI.Workflows;

namespace Indice.Features.Agents.Core.Workflows.Steps.Cases;

/// <summary>
/// Step 3 of the Cases workflow: Validates user's ownership confirmation input.
/// Compares user input with the actual case data field. Supports up to 2 validation attempts.
/// Note: User input is retrieved from the chat message history available in the workflow context.
/// </summary>
public sealed class UserInputValidator : Executor<OwnershipVerificationOutput, UserInputValidationOutput>
{
    private const int MaxValidationAttempts = 2;

    /// <summary>Creates a new <see cref="UserInputValidator"/>.</summary>
    public UserInputValidator() : base(nameof(UserInputValidator))
    {
    }

    /// <inheritdoc/>
    public override async ValueTask<UserInputValidationOutput> HandleAsync(
        OwnershipVerificationOutput verificationData,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (verificationData is null)
            throw new ArgumentNullException(nameof(verificationData));

        // In a real scenario, user input would be captured from the chat message in the conversation.
        // For now, we simulate getting it from a placeholder; actual implementation would parse
        // the next user message in the conversation flow.
        // 
        // For demonstration purposes, this returns ValidationAttempt 1.
        // In production, you'd increment based on previous attempts stored in workflow state.

        var userInput = string.Empty; // Would be populated from chat message queue
        var attempt = 1; // In production: read from context state to support retries

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
