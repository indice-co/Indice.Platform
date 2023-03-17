using IdentityServer4.Events;
using Indice.Features.Identity.SignInLogs.Models;

namespace Indice.Features.Identity.SignInLogs;

internal class SignInLogEntryFactory
{
    public static SignInLogEntry CreateFromTokenIssuedSuccessEvent(TokenIssuedSuccessEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        ApplicationId = @event.ClientId,
        ApplicationName = @event.ClientName,
        Description = "A token was successfully issued.",
        ExtraData = new { @event.GrantType, @event.ProcessId, @event.RedirectUri, @event.Scopes, @event.Tokens },
        IpAddress = @event.RemoteIpAddress,
        ResourceId = @event.Endpoint,
        ResourceType = "IdentityServer",
        SubjectId = @event.SubjectId,
        Succedded = true
    };

    public static SignInLogEntry CreateFromTokenIssuedFailureEvent(TokenIssuedFailureEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        ApplicationId = @event.ClientId,
        ApplicationName = @event.ClientName,
        Description = "A token failed to issue.",
        ExtraData = new { @event.Error, @event.ErrorDescription, @event.GrantType, @event.ProcessId, @event.RedirectUri, @event.Scopes },
        IpAddress = @event.RemoteIpAddress,
        ResourceId = @event.Endpoint,
        ResourceType = "IdentityServer",
        SubjectId = @event.SubjectId,
        Succedded = false
    };

    public static SignInLogEntry CreateFromUserLoginSuccessEvent(UserLoginSuccessEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        ApplicationId = @event.ClientId,
        Description = "A user was successfully authenticated.",
        ExtraData = new { @event.ProcessId, @event.Provider },
        IpAddress = @event.RemoteIpAddress,
        ResourceId = @event.Endpoint,
        ResourceType = "IdentityServer",
        SignInType = SignInType.Interactive,
        SubjectId = @event.SubjectId,
        SubjectName = @event.DisplayName,
        Succedded = true
    };

    public static SignInLogEntry CreateFromUserLoginFailureEvent(UserLoginFailureEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        ApplicationId = @event.ClientId,
        Description = "A user failed to authenticate.",
        ExtraData = new { @event.ProcessId },
        IpAddress = @event.RemoteIpAddress,
        ResourceId = @event.Endpoint,
        ResourceType = "IdentityServer",
        SignInType = SignInType.Interactive,
        SubjectId = @event.Username,
        SubjectName = @event.Username,
        Succedded = false
    };
}
