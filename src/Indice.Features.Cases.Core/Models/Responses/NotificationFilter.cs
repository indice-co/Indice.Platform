using System.Security.Claims;
using Indice.Security;
using Indice.Types;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription filter options.</summary>
public class NotificationFilter
{
    /// <summary>Subscriber email.</summary>
    public string[] Email { get; set; } = [];

    /// <summary>Subscriber group Id.</summary>
    public string[] GroupId { get; set; } = [];

    /// <summary>Subscriber casetype Ids.</summary>
    public Guid[] CaseTypeIds { get; set; } = [];

    /// <summary>Construct an instance from ClaimsPrincipal</summary>
    public static NotificationFilter FromUser(ClaimsPrincipal user, string groupIdClaimType) {
        var groupIds = user.FindAll(groupIdClaimType)
            .Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        var emails = user.FindAll(BasicClaimTypes.Email)
            .Select(x => x.Value).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (groupIds.Length == 0 && emails.Length == 0) {
            throw new BusinessException("Failed to create NotificationFilter.");
        }
        return new NotificationFilter {
            GroupId = groupIds,
            Email = emails
        };
    }
}