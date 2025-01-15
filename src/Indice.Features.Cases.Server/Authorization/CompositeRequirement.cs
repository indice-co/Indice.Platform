using Microsoft.AspNetCore.Authorization;

namespace Indice.Features.Cases.Server.Authorization;

public class CompositeRequirement : IAuthorizationRequirement
{
    public IEnumerable<IAuthorizationRequirement> Requirements { get; }
    
    public CompositeRequirement() {}
    
    public CompositeRequirement(IEnumerable<IAuthorizationRequirement> requirements)
    {
        Requirements = requirements;
    }
    
}

public class CompositeRequirementHandler : AuthorizationHandler<CompositeRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CompositeRequirement requirement) {
        foreach (var req in requirement.Requirements) {
            var handlerContext = new AuthorizationHandlerContext(new[] { req }, context.User, context.Resource);

            foreach (var handler in context.Requirements.OfType<IAuthorizationHandler>()) {
                await handler.HandleAsync(handlerContext);
            }

            if (!handlerContext.HasSucceeded) {
                // Stop if any requirement fails
                return;
            }
        }

        context.Succeed(requirement);
    }
}