using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Core.Models;

/// <summary>Represents a subscriber with an email address and an optional group association.</summary>
public class Subscriber
{
    /// <summary>The subscriber's email address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>The group identifier associated with the subscriber, if any.</summary>
    public string? GroupId { get; set; }

    /// <summary>Construct an instance from ClaimsPrincipal</summary>
    public static Subscriber FromUser(ClaimsPrincipal user, string groupIdClaimType) {
        var groupId = user.FindFirstValue(groupIdClaimType);
        var email = user.FindFirstValue(BasicClaimTypes.Email);

        return new Subscriber {
            GroupId = groupId,
            Email = email!
        };
    }

    /// <summary>
    /// Determines whether the email address is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <returns>true if the email address is null, empty, or contains only white-space characters; otherwise, false.</returns>
    public bool IsEmpty() => string.IsNullOrWhiteSpace(Email);

    /// <summary>
    /// Creates a new Subscriber instance that is a copy of the current instance.
    /// </summary>
    /// <returns>A new Subscriber object with the same Email and GroupId values as the current instance.</returns>
    public Subscriber Clone() => new() {
        Email = Email,
        GroupId = GroupId
    };
}