using IdentityServer4.Events;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogEntryFactory
{
    //private static readonly string INDICE_IP = "212.205.254.62";
    private static readonly string INDICE_IP = "51.107.83.216";

    public static SignInLogEntry CreateFromTokenIssuedSuccessEvent(TokenIssuedSuccessEvent @event) {
        var logEntry = new SignInLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A token was successfully issued.",
            GrantType = @event.GrantType,
#if DEBUG
            IpAddress = INDICE_IP,
#else
            IpAddress = @event.RemoteIpAddress,
#endif
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            Succeeded = true,
            ExtraData = new SignInLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.RedirectUri = @event.RedirectUri;
        logEntry.ExtraData.Scope = @event.Scopes;
        logEntry.ExtraData.Tokens = @event.Tokens.Select(x => new SignInLogEntryToken { 
            TokenType = x.TokenType, 
            TokenValue = x.TokenValue 
        });
        return logEntry;
    }

    public static SignInLogEntry CreateFromTokenIssuedFailureEvent(TokenIssuedFailureEvent @event) {
        var logEntry = new SignInLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            ApplicationName = @event.ClientName,
            Description = "A token failed to issue.",
            GrantType = @event.GrantType,
#if DEBUG
            IpAddress = INDICE_IP,
#else
            IpAddress = @event.RemoteIpAddress,
#endif
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SubjectId = @event.SubjectId,
            Succeeded = false,
            ExtraData = new SignInLogEntryExtraData()
        };
        logEntry.ExtraData.Error = @event.Error;
        logEntry.ExtraData.ErrorDescription = @event.ErrorDescription;
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.RedirectUri = @event.RedirectUri;
        logEntry.ExtraData.Scope = @event.Scopes;
        return logEntry;
    }

    public static SignInLogEntry CreateFromUserLoginSuccessEvent(UserLoginSuccessEvent @event) {
        var logEntry = new SignInLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            Description = "A user was successfully authenticated.",
#if DEBUG
            IpAddress = INDICE_IP,
#else
            IpAddress = @event.RemoteIpAddress,
#endif
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SignInType = SignInType.Interactive,
            SubjectId = @event.SubjectId,
            SubjectName = @event.DisplayName,
            Succeeded = true,
            ExtraData = new SignInLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        logEntry.ExtraData.Provider = @event.Provider;
        return logEntry;
    }

    public static SignInLogEntry CreateFromUserLoginFailureEvent(UserLoginFailureEvent @event) {
        var logEntry = new SignInLogEntry(Guid.NewGuid(), DateTimeOffset.UtcNow) {
            ActionName = @event.Name,
            ApplicationId = @event.ClientId,
            Description = "A user failed to authenticate.",
#if DEBUG
            IpAddress = INDICE_IP,
#else
            IpAddress = @event.RemoteIpAddress,
#endif
            ResourceId = @event.Endpoint,
            ResourceType = "IdentityServer",
            SignInType = SignInType.Interactive,
            SubjectId = @event.Username,
            SubjectName = @event.Username,
            Succeeded = false,
            ExtraData = new SignInLogEntryExtraData()
        };
        logEntry.ExtraData.ProcessId = @event.ProcessId;
        return logEntry;
    }
}
