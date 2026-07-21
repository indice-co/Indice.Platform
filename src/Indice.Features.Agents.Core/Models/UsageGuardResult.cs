using Indice.Features.Agents.Core.Services;

namespace Indice.Features.Agents.Core.Models;

/// <summary>
/// Outcome of a <see cref="IUsageGuardService"/> check. The guard is transport-agnostic — it reports whether the
/// action is allowed and, when denied, carries the predefined user-facing text; the caller decides how to surface it
/// (in-chat reply or error response).
/// </summary>
public class UsageGuardResult
{
    /// <summary>True when the checked action may proceed.</summary>
    public bool Allowed { get; init; }

    /// <summary>Predefined user-facing text explaining the denial; <c>null</c> when <see cref="Allowed"/> is true.</summary>
    public string? Message { get; init; }

    /// <summary>Creates an allowing result.</summary>
    public static UsageGuardResult Allow() => new() { Allowed = true };

    /// <summary>Creates a denying result carrying the predefined <paramref name="message"/>.</summary>
    public static UsageGuardResult Deny(string message) => new() { Allowed = false, Message = message };
}
