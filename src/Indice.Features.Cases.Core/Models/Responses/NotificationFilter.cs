using System.Security.Claims;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription filter options.</summary>
public class NotificationFilter
{
    /// <summary>Subscriber email.</summary>
    public List<string> Email { get; set; } = [];

    /// <summary>Subscriber group Id.</summary>
    public List<string> GroupId { get; set; } = [];

    /// <summary>Subscriber casetype Ids.</summary>
    public List<Guid> CaseTypeIds { get; set; } = [];

    /// <summary>Construct an instance from ClaimsPrincipal</summary>
    public static NotificationFilter FromUser(ClaimsPrincipal user, string groupIdClaimType) {
        var subscriber = Subscriber.FromUser(user, groupIdClaimType);

        return new NotificationFilter {
            GroupId = subscriber.GroupId is null ? [] : [subscriber.GroupId],
            Email = [subscriber.Email]
        };
    }
}