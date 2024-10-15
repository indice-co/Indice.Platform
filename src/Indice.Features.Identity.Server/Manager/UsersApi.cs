using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Http.Filters;
using Indice.Features.Identity.Server;
using Indice.Features.Identity.Server.Manager;
using Indice.Features.Identity.Server.Manager.Models;
using Indice.Security;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

/// <summary>Contains operations for managing application users.</summary>
public static class UsersApi
{
    /// <summary>Adds endpoints for managing application users.</summary>
    /// <param name="routes">Indice Identity Server route builder.</param>
    public static RouteGroupBuilder MapManageUsers(this IdentityServerEndpointRouteBuilder routes) {
        var options = routes.GetEndpointOptions();
        var group = routes.MapGroup($"{options.ApiPrefix}/users");
        group.WithTags("Users");
        group.WithGroupName("identity");
        // Add security requirements, all incoming requests to this API *must* be authenticated with a valid user.
        var allowedScopes = new[] { options.ApiScope, IdentityEndpoints.SubScopes.Users }.Where(x => x != null).Cast<string>().ToArray();
        group.RequireAuthorization(policy => policy
             .RequireAuthenticatedUser()
             .AddAuthenticationSchemes(IdentityEndpoints.AuthenticationScheme)
             .RequireClaim(BasicClaimTypes.Scope, allowedScopes)
        );
        group.WithOpenApi().AddOpenApiSecurityRequirement("oauth2", allowedScopes);
        group.ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, UserHandlers.GetUsers)
             .WithName(nameof(UserHandlers.GetUsers))
             .WithSummary($"Returns a list of {nameof(UserInfo)} objects containing the total number of users in the database and the data filtered according to the provided ListOptions.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapGet("{userId}", UserHandlers.GetUser)
             .WithName(nameof(UserHandlers.GetUser))
             .WithSummary("Gets a user by it's unique id.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutputMemory(expiration: 1);

        group.MapPost("", UserHandlers.CreateUser)
             .WithName(nameof(UserHandlers.CreateUser))
             .WithSummary("Creates a new user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .WithParameterValidation<CreateUserRequest>();

        group.MapPut("{userId}", UserHandlers.UpdateUser)
             .WithName(nameof(UserHandlers.UpdateUser))
             .WithSummary("Updates an existing user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .WithParameterValidation<UpdateUserRequest>()
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapDelete("{userId}", UserHandlers.DeleteUser)
             .WithName(nameof(UserHandlers.DeleteUser))
             .WithSummary("Permanently deletes an existing user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .CacheOutputMemory();

        group.MapPost("{userId}/email/confirmation", UserHandlers.ResendConfirmationEmail)
             .WithName(nameof(UserHandlers.ResendConfirmationEmail))
             .WithSummary("Resends the confirmation email for a given user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        group.MapPost("{userId}/roles/{roleId}", UserHandlers.AddUserRole)
             .WithName(nameof(UserHandlers.AddUserRole))
             .WithSummary("Adds a new role to the specified user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapDelete("{userId}/roles/{roleId}", UserHandlers.DeleteUserRole)
             .WithName(nameof(UserHandlers.DeleteUserRole))
             .WithSummary("Removes an existing role from the specified user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapGet("{userId}/claims/{claimId}", UserHandlers.GetUserClaim)
             .WithName(nameof(UserHandlers.GetUserClaim))
             .WithSummary("Gets a specified claim for a given user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader)
             .CacheOutputMemory();

        group.MapPost("{userId}/claims", UserHandlers.AddUserClaim)
             .WithName(nameof(UserHandlers.AddUserClaim))
             .WithSummary("Adds a claim for the specified user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .WithParameterValidation<CreateClaimRequest>()
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapPut("{userId}/claims/{claimId}", UserHandlers.UpdateUserClaim)
             .WithName(nameof(UserHandlers.UpdateUserClaim))
             .WithSummary("Updates an existing user claim.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .WithParameterValidation<CreateUserRequest>()
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapDelete("{userId}/claims/{claimId}", UserHandlers.DeleteUserClaim)
             .WithName(nameof(UserHandlers.DeleteUserClaim))
             .WithSummary("Permanently deletes a specified claim from a user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapGet("{userId}/applications", UserHandlers.GetUserApplications)
             .WithName(nameof(UserHandlers.GetUserApplications))
             .WithSummary("Gets a list of the applications the user has given consent to or currently has IdentityServer side tokens for.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapGet("{userId}/devices", UserHandlers.GetUserDevices)
             .WithName(nameof(UserHandlers.GetUserDevices))
             .WithSummary("Gets a list of the devices of the specified user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapGet("{userId}/external-logins", UserHandlers.GetUserExternalLogins)
             .WithName(nameof(UserHandlers.GetUserExternalLogins))
             .WithSummary("Gets a list of the external login providers for the specified user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersReader);

        group.MapDelete("{userId}/external-logins/{provider}", UserHandlers.DeleteUserExternalLogin)
             .WithName(nameof(UserHandlers.DeleteUserExternalLogin))
             .WithSummary("Permanently deletes a specified login provider association from a user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter);

        group.MapPut("{userId}/block", UserHandlers.SetUserBlock)
             .WithName(nameof(UserHandlers.SetUserBlock))
             .WithSummary("Toggles user block state.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser))
             .WithParameterValidation<SetUserBlockRequest>();

        group.MapPut("{userId}/unlock", UserHandlers.UnlockUser)
             .WithName(nameof(UserHandlers.UnlockUser))
             .WithSummary("Unlocks a user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser));

        group.MapPut("{userId}/set-password", UserHandlers.SetPassword)
             .WithName(nameof(UserHandlers.SetPassword))
             .WithSummary("Sets the password for a given user.")
             .RequireAuthorization(IdentityEndpoints.Policies.BeUsersWriter)
             .InvalidateCache(nameof(UserHandlers.GetUser))
             .WithParameterValidation<SetPasswordRequest>();


        return group;
    }
}
