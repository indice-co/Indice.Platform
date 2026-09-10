
namespace Indice.Features.Agents.Core;

/// <summary>Provides localization for agent messages.</summary>
public class AgentMessageLocalizer
{
    private static string GetResourceOrDefault(string key, string fallback) => AgentResources.ResourceManager.GetString(key, AgentResources.Culture) ?? fallback;

    /// <summary>Step label shown while intent is classified.</summary>
    public virtual string StepIntentClassifier => AgentResources.StepIntentClassifier;

    /// <summary>Step label shown while query is rewritten.</summary>
    public virtual string StepQueryRewriter => AgentResources.StepQueryRewriter;

    /// <summary>Step label shown while context is retrieved.</summary>
    public virtual string StepRetriever => AgentResources.StepRetriever;

    /// <summary>Step label shown while results are ranked.</summary>
    public virtual string StepReranker => AgentResources.StepReranker;

    /// <summary>Step label shown while answer is composed.</summary>
    public virtual string StepAnswerComposer => AgentResources.StepAnswerComposer;

    /// <summary>Step label shown while a direct purpose response is generated.</summary>
    public virtual string StepPurposeResponder => AgentResources.StepPurposeResponder;

    /// <summary>Step label shown while out-of-scope response is prepared.</summary>
    public virtual string StepOutOfScopeResponder => AgentResources.StepOutOfScopeResponder;

    /// <summary>Step label shown while retrieving case data.</summary>
    public virtual string StepCaseDataRetriever => GetResourceOrDefault("StepCaseDataRetriever", "Retrieve Case Data");

    /// <summary>Step label shown while verifying case ownership details.</summary>
    public virtual string StepOwnershipVerifier => GetResourceOrDefault("StepOwnershipVerifier", "Verify ownership of Case Data");

    /// <summary>Step label shown while sending OTP code.</summary>
    public virtual string StepOtpAgent => GetResourceOrDefault("StepOtpAgent", "Send OTP");

    /// <summary>Step label shown while validating OTP code.</summary>
    public virtual string StepOtpCodeValidator => GetResourceOrDefault("StepOtpCodeValidator", "Verify OTP code");

    /// <summary>Step label shown while preparing OTP retry prompt.</summary>
    public virtual string StepOtpRetryChallengeBuilder => GetResourceOrDefault("StepOtpRetryChallengeBuilder", "Prepare OTP retry");

    /// <summary>Step label shown while presenting case details.</summary>
    public virtual string StepCaseDataPresenter => GetResourceOrDefault("StepCaseDataPresenter", "Present case details");

    /// <summary>Step label shown while validating ownership confirmation.</summary>
    public virtual string StepOwnershipValidator => GetResourceOrDefault("StepOwnershipValidator", "Validating ownership confirmation");

    /// <summary>Step label shown while sending OTP in workflow step.</summary>
    public virtual string StepOtpCodeSend => GetResourceOrDefault("StepOtpCodeSend", "Send OTP");

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

    /// <summary>
    /// Message to display to user for Ownership verification failure when retrying
    /// </summary>
    public virtual string OwnershipVerificationFailedMaxAttemptsMessage(int maxAttempts) => string.Format(AgentResources.OwnershipVerificationFailedMaxAttemptsMessage, maxAttempts);

}
