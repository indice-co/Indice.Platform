using Microsoft.AspNetCore.Authorization;

namespace Indice.Features.Cases.Server.Authorization;

public class NotAnonymousUserRequirement : IAuthorizationRequirement
{
    public NotAnonymousUserRequirement() {}
}

public class NotAnonymousUserHandler : AuthorizationHandler<NotAnonymousUserRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotAnonymousUserRequirement requirement) {
        if (context.User.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated)) {
            return Task.CompletedTask;
        }
        
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}