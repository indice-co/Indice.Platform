using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Features.Agents.Core.Models.Cases;

/// <summary>Durable resume state for the customer-data/cases verification workflow.</summary>
public sealed class CustomerDataState
{
    /// <summary>Conversation identifier this state belongs to.</summary>
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>External reference value used to resolve customer data.</summary>
    public string? ReferenceId { get; set; }

    /// <summary>External reference type/classifier (e.g. case number type).</summary>
    public string? ReferenceType { get; set; }

    /// <summary>Current workflow stage used for cross-turn resume.</summary>
    public CustomerDataStage Stage { get; set; } = CustomerDataStage.Start;

    /// <summary>Last executor id that updated this state.</summary>
    public string? LastStepId { get; set; }

    /// <summary>Pending ownership verification payload used to resume the next turn.</summary>
    public OwnershipVerificationOutput? PendingOwnershipVerification { get; set; }

    /// <summary>Pending OTP challenge payload used to resume the next turn.</summary>
    public OtpChallengeOutput? PendingOtpChallenge { get; set; }

    /// <summary>Whether ownership/identity verification has completed successfully.</summary>
    public bool IsIdentityVerified { get; set; }

    /// <summary>Whether OTP verification has completed successfully.</summary>
    public bool IsOtpVerified { get; set; }

    /// <summary>Chosen channel path to send OTP (masked channel rendering can use this).</summary>
    public string? ChannelPath { get; set; }

    /// <summary>Failed ownership verification attempts.</summary>
    public int FailedOwnershipAttempts { get; set; }

    /// <summary>Consumed ownership validation attempts.</summary>
    public int OwnershipAttempts { get; set; }

    /// <summary>Failed OTP verification attempts.</summary>
    public int FailedOtpAttempts { get; set; }

    /// <summary>Last update UTC timestamp.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Parses JSON into <see cref="CustomerDataState"/> safely.</summary>
    public static CustomerDataState? FromJson(string? json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return null;
        }
        try {
            return JsonSerializer.Deserialize<CustomerDataState>(json);
        } catch {
            return null;
        }
    }

    /// <summary>Serializes this state to JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(this);
}

/// <summary>Cross-turn stages for the customer-data workflow.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CustomerDataStage
{
    /// <summary>Initial stage; reference may need collection/normalization.</summary>
    Start = 0,
    /// <summary>Waiting for ownership/identity answer from user.</summary>
    AwaitOwnershipConfirmation = 1,
    /// <summary>Waiting for OTP code from user.</summary>
    AwaitOtpCode = 2,
    /// <summary>Terminal completed stage.</summary>
    Completed = 3,
    /// <summary>Terminal failed stage.</summary>
    Failed = 4
}
