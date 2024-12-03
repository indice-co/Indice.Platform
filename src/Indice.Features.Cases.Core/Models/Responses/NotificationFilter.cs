using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription filter options.</summary>
public class NotificationFilter
{
    /// <summary>Subscriber email.</summary>
    public string[] Email { get; set; } = Array.Empty<string>();

    /// <summary>Subscriber group Id.</summary>
    public string[] GroupId { get; set; } = Array.Empty<string>();

    /// <summary>Subscriber casetype Ids.</summary>
    public Guid[] CaseTypeIds { get; set; } = Array.Empty<Guid>();

    /// <summary>Construct an instance from ClaimsPrincipal</summary>
    public static NotificationFilter FromUser(ClaimsPrincipal user, string groupIdClaimType) {
        var groupIds = user.FindAll(groupIdClaimType)
            .Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        var emails = user.FindAll(BasicClaimTypes.Email)
            .Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (groupIds.Length == 0 && emails.Length == 0) {
            throw new Exception($"Failed to create NotificationFilter.");
        }
        return new NotificationFilter {
            GroupId = groupIds,
            Email = emails
        };
    }
}