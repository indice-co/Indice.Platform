
namespace Indice.Features.Agents.Core;

/// <summary>Provides localization for agent messages.</summary>
public class AgentMessageLocalizer
{
    /// <summary>Text for the email label.</summary>
    public virtual string OwnershipRetryPrompt => AgentResources.OwnershipRetryPrompt;

    /// <summary>Error message when user exceed max verification attempts.</summary>
    public virtual string VerificationFailedMaxAttempts(int maxAttempts) => string.Format(AgentResources.VerificationFailedMaxAttempts, maxAttempts);

    /// <summary>Error message when user fails a verification attempt but has remaining attempts.</summary>
    public virtual string VerificationFailedRetry(int attempt, int maxAttempts) => string.Format(AgentResources.VerificationFailedRetry, attempt, maxAttempts);

    /// <summary>Message prompt for ownership verification.</summary>
    public virtual string OwnershipVerificationMessagePrompt => AgentResources.OwnershipVerificationMessagePrompt;

    /// <summary>
    /// Message to display to user for OTP Validation
    /// </summary>
    /// <param name="input">The email or phone number where the OTP was send. The field should be masked</param>
    /// <returns></returns>
    public virtual string OtpVerificationCodeSendMessage(string input) => string.Format(AgentResources.OtpVerificationCodeSendMessage, input);

    /// <summary> Message to display to user for empty OTP input</summary>
        public virtual string OtpInputValidationEmpty => AgentResources.OtpInputValidationEmpty;
    /// <summary>
    /// Message to display to user for OTP Validation Success
    /// </summary>
    public virtual string OtvpVerificationSuccessMessage => AgentResources.OtvpVerificationSuccessMessage;
    /// <summary>
    /// Message to display to user for OTP Validation failure
    /// </summary>
    public virtual string OtvpVerificationFailedMessage => AgentResources.OtvpVerificationFailedMessage;

    /// <summary>
    /// Message to display to user for OTP Validation failure when max attempts reached
    /// </summary>
    public virtual string InvalidOtpMaxAttemptsReachedMessage => AgentResources.InvalidOtpMaxAttemptsReachedMessage;

    /// <summary>
    /// Message to display to user for OTP Validation failure when retrying
    /// </summary>
    public virtual string InvalidOtpRetryMessage(int attempts) => string.Format(AgentResources.InvalidOtpRetryMessage, attempts);
}
