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
    public static ActivityLogEntry CreateFromTokenIssuedSuccessEvent(TokenIssuedSuccessEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            EventType = ActivityLogEventType.TokenIssued,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A token was successfully issued.",
            GrantType = @event.GrantType,
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            Succeeded = true,
            ExtraData = new ActivityLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.RedirectUri = @event.RedirectUri;
        logEntry.ExtraData.Scope = @event.Scopes;
        logEntry.ExtraData.Tokens = @event.Tokens.Select(x => new ActivityLogEntryToken {
            TokenType = x.TokenType,
            TokenValue = x.TokenValue
        });
        logEntry.ExtraData.OriginalEventType = nameof(TokenIssuedSuccessEvent);
        return logEntry;
    }

    public static ActivityLogEntry CreateFromTokenIssuedFailureEvent(TokenIssuedFailureEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            EventType = ActivityLogEventType.TokenIssued,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A token failed to issue.",
            GrantType = @event.GrantType,
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            Succeeded = false,
            ExtraData = new ActivityLogEntryExtraData()
        };
        logEntry.ExtraData.Error = @event.Error;
        logEntry.ExtraData.ErrorDescription = @event.ErrorDescription;
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.RedirectUri = @event.RedirectUri;
        logEntry.ExtraData.Scope = @event.Scopes;
        logEntry.ExtraData.OriginalEventType = nameof(TokenIssuedFailureEvent);
        return logEntry;
    }

    public static ActivityLogEntry CreateFromUserLoginSuccessEvent(ExtendedUserLoginSuccessEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            EventType = ActivityLogEventType.UserLoginCompleted,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A user was successfully logged in.",
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            ActivityType = ActivityType.Interactive,
            SubjectId = @event.SubjectId,
            SubjectName = @event.DisplayName,
            SessionId = @event.SessionId,
            Succeeded = true,
            ExtraData = new ActivityLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.Provider = @event.Provider;
        if (@event.Warning is not null) {
            logEntry.Review = true;
            logEntry.ExtraData.Warning = @event.Warning.Value;
        }
        if (@event.AuthenticationMethods.Any()) {
            logEntry.ExtraData.AuthenticationMethods = @event.AuthenticationMethods;
        }
        logEntry.ExtraData.OriginalEventType = nameof(ExtendedUserLoginSuccessEvent);
        return logEntry;
    }

    public static ActivityLogEntry CreateFromUserLoginFailureEvent(ExtendedUserLoginFailureEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            EventType = ActivityLogEventType.UserLoginCompleted,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A user failed to authenticate.",
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            ActivityType = ActivityType.Interactive,
            SubjectId = @event.SubjectId,
            SubjectName = @event.Username,
            Succeeded = false,
            ExtraData = new ActivityLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.OriginalEventType = nameof(ExtendedUserLoginFailureEvent);
        return logEntry;
    }

    public static ActivityLogEntry CreateFromUserPasswordLoginSuccessEvent(UserPasswordLoginSuccessEvent @event) {
        var logEntry = new ActivityLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            EventType = ActivityLogEventType.UserPasswordValidationCompleted,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A user was successfully provided his credentials.",
            IpAddress = @event.RemoteIpAddress,
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            ActivityType = ActivityType.Interactive,
            SubjectId = @event.SubjectId,
            SubjectName = @event.DisplayName,
            Succeeded = true,
            ExtraData = new ActivityLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        if (@event.Warning is not null) {
            logEntry.Review = true;
            logEntry.ExtraData.Warning = @event.Warning.Value;
        }
        logEntry.ExtraData.OriginalEventType = nameof(UserPasswordLoginSuccessEvent);
        return logEntry;
    }
}
