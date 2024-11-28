using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription.</summary>
public class NotificationSubscription
{
    /// <summary>The notification subscription CaseType Id.</summary>
    public Guid CaseTypeId { get; set; }

    /// <summary>Subscriber email.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Subscriber group Id.</summary>
    public string? GroupId { get; set; }

    /// <summary>Construct an instance from ClaimsPrincipal</summary>
    internal static NotificationSubscription FromUser(ClaimsPrincipal user, string groupIdClaimType) {
        var groupId = user.FindFirstValue(groupIdClaimType);
        var email = user.FindFirstValue(BasicClaimTypes.Email);
        return new NotificationSubscription {
            GroupId = groupId,
            Email = email!
        };
    }
}