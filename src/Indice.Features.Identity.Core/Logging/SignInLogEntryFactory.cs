using IdentityServer4.Events;
using Indice.Features.Identity.Core.Logging.Models;

namespace Indice.Features.Identity.Core.Logging;

internal class SignInLogEntryFactory
{
    public static SignInLogEntry CreateFromTokenIssuedSuccessEvent(TokenIssuedSuccessEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        Description = "A token was successfully issued.",
        ExtraData = new {
            @event.ActivityId,
            @event.GrantType,
            @event.ProcessId,
            @event.RedirectUri,
            @event.Scopes,
            @event.Tokens
        },
        ResourceType = "IdentityServer",
        ResourceId = @event.Endpoint,
        Subject = @event.ClientName,
        SubjectId = @event.ClientId,
        SubjectType = "Machine",
        Succedded = true
    };

    public static SignInLogEntry CreateFromTokenIssuedFailureEvent(TokenIssuedFailureEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        Description = "A token failed to issue.",
        ExtraData = new {
            @event.ActivityId,
            @event.Error,
            @event.ErrorDescription,
            @event.ProcessId
        },
        ResourceType = "IdentityServer",
        ResourceId = @event.Endpoint,
        Subject = @event.ClientName,
        SubjectId = @event.ClientId,
        SubjectType = "Machine",
        Succedded = false
    };

    public static SignInLogEntry CreateFromUserLoginSuccessEvent(UserLoginSuccessEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        Description = "A user was successfully authenticated.",
        ExtraData = new {
            @event.ActivityId,
            @event.ProcessId,
            @event.Provider
        },
        ResourceType = "IdentityServer",
        ResourceId = @event.Endpoint,
        Subject = @event.DisplayName,
        SubjectId = @event.SubjectId,
        SubjectType = "User",
        Succedded = true
    };

    public static SignInLogEntry CreateFromUserLoginFailureEvent(UserLoginFailureEvent @event) => new(Guid.NewGuid(), DateTimeOffset.UtcNow) {
        ActionName = @event.Name,
        Description = "A user failed to authenticate.",
        ExtraData = new {
            @event.ActivityId,
            @event.ProcessId
        },
        ResourceType = "IdentityServer",
        ResourceId = @event.Endpoint,
        Subject = @event.Username,
        SubjectId = @event.Username,
        SubjectType = "User",
        Succedded = false
    };
}
