namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>The deterministic phase of the Cases state machine for a conversation.</summary>
public enum CasesReplayPhase
{
    /// <summary>No prior turns exist; the next run starts the workflow from scratch.</summary>
    Start = 0,
    /// <summary>The workflow emitted the ownership verification prompt and awaits the user's confirmation value.</summary>
    AwaitOwnershipConfirmation = 1,
    /// <summary>The workflow emitted the OTP challenge prompt and awaits the user's OTP code.</summary>
    AwaitOtpCode = 2
}

/// <summary>
/// Persisted state for the Cases conversation state machine. Each turn runs a fresh workflow whose entry router
/// uses this state to start execution at the correct phase, feeding the pending payload plus the user's reply
/// directly into the next step. Terminal steps clear the state.
/// </summary>
public class CasesReplayState
{
    /// <summary>The conversation identifier this state belongs to.</summary>
    public required string ConversationId { get; init; }
    /// <summary>The current phase of the state machine.</summary>
    public CasesReplayPhase Phase { get; set; } = CasesReplayPhase.Start;
    /// <summary>The pending ownership verification payload, persisted when the ownership prompt was emitted.</summary>
    public OwnershipVerificationOutput? PendingOwnershipVerification { get; set; }
    /// <summary>The pending OTP challenge payload, persisted when the OTP prompt was emitted.</summary>
    public OtpChallengeOutput? PendingOtpChallenge { get; set; }
    /// <summary>Number of ownership validation attempts consumed so far.</summary>
    public int OwnershipAttempts { get; set; }
    /// <summary>When the state was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
