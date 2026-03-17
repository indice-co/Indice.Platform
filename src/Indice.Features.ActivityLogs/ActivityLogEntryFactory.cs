#if NET9_0_OR_GREATER
using Duende.IdentityServer.Events;
#else
using IdentityServer4.Events;
#endif
using Indice.Features.Identity.Core.Events;
using Indice.Features.ActivityLogs.Models;

namespace Indice.Features.ActivityLogs;

internal class ActivityLogEntryFactory
{

    public static ActivityLogEntry CreateFromUserLoginSuccessEvent(ExtendedUserLoginSuccessEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A user was successfully logged in.",
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            SubjectName = @event.DisplayName,
            SessionId = @event.SessionId,
            Succeeded = true,
            ExtraData = new ActivityLogEntryExtraData()
        };
        return logEntry;
    }

    public static ActivityLogEntry CreateFromUserPasswordLoginSuccessEvent(UserPasswordLoginSuccessEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A user was successfully provided his credentials.",
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            SubjectName = @event.DisplayName,
            Succeeded = true,
            ExtraData = new ActivityLogEntryExtraData()
        };
        return logEntry;
    }
}
