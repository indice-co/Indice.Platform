using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Authorization;

/// <summary>
/// Handles the requirement that an endpoint is accessible only to the customers that take part in the case, as its owner, co-owner or creator,
/// and only when their permission on the case reaches the one the endpoint asks for.
/// </summary>
public class CasesAccessOwnerHandler : AuthorizationHandler<CasesOwnerAccessRequirement>, IAuthorizationRequirement {
    private readonly ICaseContactPermissionService _contactPermissionService;
    private readonly CasesDbContext _dbContext;
    private readonly ILogger<CasesAccessOwnerHandler> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CasesOptions _casesOptions;

    /// <summary>
    /// Creates a new instance of <see cref="CasesAccessOwnerHandler"/>.
    /// </summary>
    /// <param name="contactPermissionService">Resolves and caches the permission of a customer on a case.</param>
    /// <param name="casesOptions"></param>
    /// <param name="dbContext"></param>
    /// <param name="logger"></param>
    /// <param name="httpContextAccessor"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public CasesAccessOwnerHandler(
        ICaseContactPermissionService contactPermissionService,
        IOptions<CasesOptions> casesOptions,
        CasesDbContext dbContext,
        ILogger<CasesAccessOwnerHandler> logger,
        IHttpContextAccessor httpContextAccessor) {
        _contactPermissionService = contactPermissionService ?? throw new ArgumentNullException(nameof(contactPermissionService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _casesOptions = casesOptions?.Value ?? throw new ArgumentNullException(nameof(casesOptions));
    }

    /// <inheritdoc/>
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CasesOwnerAccessRequirement requirement) {
        var userIsAnonymous = context.User?.Identity == null || !context.User.Identities.Any(identity => identity.IsAuthenticated);
        if (userIsAnonymous) {
            _logger.LogInformation("Request is unauthorized.");
            return;
        }

        var actor = context.User!.UserToActor(_casesOptions);
        var routeData = _httpContextAccessor.HttpContext!.GetRouteData();
        if (!Guid.TryParse((string?)routeData.Values["caseId"], out var caseId)) {
            // If you cannot determine if requirement succeeded or not, please do nothing.
            return;
        }

        // The cached value is the permission of the user on the case, not the answer to one requirement,
        // so it is compared with the permission that this endpoint asks for on every request.
        var permission = await _contactPermissionService.GetPermission(actor.Id, caseId);
        if (permission >= requirement.MinimumPermission) {
            context.Succeed(requirement);
            return;
        }
        // Let the request for a case that does not exist through, so that the endpoint answers with its own not found.
        var caseExists = await _dbContext.Cases.AnyAsync(c => c.Id == caseId);
        if (caseExists) {
            _logger.LogInformation("User {UserId} does not have the {Permission} permission on case {CaseId}.", actor.Id, requirement.MinimumPermission, caseId);
            return;
        }
        context.Succeed(requirement);
    }
}
