using System.Security.Claims;
using Indice.Security;

namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The notification subscription.</summary>
public class NotificationSubscription
{
    /// <summary>The notification subscription CaseType Id.</summary>
    public Guid CaseTypeId { get; set; }

    /// <summary>
    /// Gets or sets the subscriber associated with this instance.
    /// </summary>
    public Subscriber Subscriber { get; set; } = new();   
}