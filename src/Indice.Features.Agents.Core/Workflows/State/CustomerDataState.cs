using Indice.Features.Agents.Core.Models;
using Indice.Features.Agents.Core.Workflows.Verification;

namespace Indice.Features.Agents.Core.Workflows.State;

/// <summary>The step of the customer-data flow a conversation is currently parked at.</summary>
public enum CustomerDataStage
{
    /// <summary>Nothing is known yet — the external reference has to be collected from the user.</summary>
    CollectReference,
    /// <summary>The reference resolved; the user must prove knowledge of something inside the payload.</summary>
    VerifyIdentity,
    /// <summary>Knowledge was proven; a one-time password is outstanding on a channel found in the payload.</summary>
    VerifyCode,
    /// <summary>Verification completed; the data can be presented.</summary>
    Present,
    /// <summary>The data was presented. A new reference restarts the flow.</summary>
    Completed,
    /// <summary>Verification failed too many times. The conversation cannot see this reference's data.</summary>
    Blocked
}

/// <summary>
/// Durable state of the customer-data sub-workflow, persisted per conversation by
/// <see cref="Services.IWorkflowStateStore"/> and read back on the next turn so the loop resumes at
/// <see cref="Stage"/> instead of starting over.
/// </summary>
/// <remarks>
/// The retrieved payload is deliberately <b>not</b> part of this record: it is fetched again on every turn
/// from the system of record, so undisclosed customer data is never persisted in the chat database. Only the
/// reference, the progress flags and the (already masked-on-display) challenge channel are kept.
/// </remarks>
public record CustomerDataState
{
    /// <summary>The external identifier the conversation is grounded on, e.g. a case number.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>The kind of record <see cref="ReferenceId"/> points at, e.g. <c>ServicePickup</c>.</summary>
    public string? ReferenceType { get; init; }

    /// <summary>The step the run stopped at, and the step the next turn resumes from.</summary>
    public CustomerDataStage Stage { get; init; } = CustomerDataStage.CollectReference;

    /// <summary>Id of the executor that last handled a turn — the resume point in workflow terms.</summary>
    public string? LastStepId { get; init; }

    /// <summary>Whether the user has proven knowledge of a value inside the payload.</summary>
    public bool IdentityVerified { get; init; }

    /// <summary>Whether the one-time password sent to <see cref="ChannelPath"/> was validated.</summary>
    public bool CodeVerified { get; init; }

    /// <summary>Kind of the channel the one-time password was delivered to.</summary>
    public VerifiableFieldKind? ChannelKind { get; init; }

    /// <summary>Path of the channel inside the payload (not the value itself, which is re-read from the source).</summary>
    public string? ChannelPath { get; init; }

    /// <summary>Failed verification attempts so far, across both challenges.</summary>
    public int FailedAttempts { get; init; }

    /// <summary>The external reference, when one has been established.</summary>
    public ExternalReference? GetReference() =>
        string.IsNullOrWhiteSpace(ReferenceId) ? null : new ExternalReference(ReferenceId, ReferenceType ?? string.Empty);
}
