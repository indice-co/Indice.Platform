using System.Net.Mime;
using System.Security.Claims;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Api.Configuration;
using Indice.AspNetCore.Identity.Api.Data;
using Indice.AspNetCore.Identity.Api.Events;
using Indice.AspNetCore.Identity.Api.Filters;
using Indice.AspNetCore.Identity.Api.Models;
using Indice.AspNetCore.Identity.Api.Security;
using Indice.Configuration;
using Indice.Events;
using Indice.Features.Identity.Core;
using Indice.Features.Identity.Core.Data;
using Indice.Features.Identity.Core.Data.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Api.Controllers;

/// <summary>Contains operations for managing applications users.</summary>
/// <response code="401">Unauthorized</response>
/// <response code="403">Forbidden</response>
/// <response code="500">Internal Server Error</response>
[ApiController]
[ApiExplorerSettings(GroupName = "identity")]
[Consumes(MediaTypeNames.Application.Json)]
[ProblemDetailsExceptionFilter]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
[ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
[Route("api/users")]
internal class UsersController : ControllerBase
{
    private readonly ExtendedUserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
    private readonly IPersistedGrantService _persistedGrantService;
    private readonly IClientStore _clientStore;
    private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
    private readonly GeneralSettings _generalSettings;
    private readonly IStringLocalizer<UsersController> _localizer;
    private readonly ExtendedConfigurationDbContext _configurationDbContext;
    private readonly IPlatformEventService _eventService;
    private readonly IConfiguration _configuration;

    /// <summary>The name of the controller.</summary>
    public const string Name = "Users";

    /// <summary>Creates an instance of <see cref="UsersController"/>.</summary>
    /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
    /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
    /// <param name="dbContext">Class for the Entity Framework database context used for identity.</param>
    /// <param name="persistedGrantService">Implements persisted grant logic.</param>
    /// <param name="clientStore">Retrieval of client configuration.</param>
    /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
    /// <param name="generalSettings">General settings for an ASP.NET Core application.</param>
    /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="UsersController"/>.</param>
    /// <param name="configurationDbContext">Extended DbContext for the IdentityServer configuration data.</param>
    /// <param name="eventService">Models the event mechanism used to raise events inside the platform.</param>
    /// <param name="configuration">Represents a set of key/value application configuration properties.</param>
    public UsersController(
        ExtendedUserManager<User> userManager,
        RoleManager<Role> roleManager,
        ExtendedIdentityDbContext<User, Role> dbContext,
        IPersistedGrantService persistedGrantService,
        IClientStore clientStore,
        IdentityServerApiEndpointsOptions apiEndpointsOptions,
        IOptions<GeneralSettings> generalSettings,
        IStringLocalizer<UsersController> localizer,
        ExtendedConfigurationDbContext configurationDbContext,
        IPlatformEventService eventService,
        IConfiguration configuration
    ) {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));
        _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
        _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
        _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>Returns a list of <see cref="UserInfo"/> objects containing the total number of users in the database and the data filtered according to the provided <see cref="ListOptions"/>.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    /// <response code="200">OK</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [HttpGet]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserInfo>))]
    public async Task<IActionResult> GetUsers([FromQuery] ListOptions<UserListFilter> options) {
        var query = _dbContext.Users.AsNoTracking();
        if (options?.Filter != null) {
            var filter = options.Filter;
            query = query.Where(x => filter.Claim == null || x.Claims.Any(x => x.ClaimType == filter.Claim.Type && x.ClaimValue == filter.Claim.Value));
        }
        // https://docs.microsoft.com/en-us/ef/core/querying/complex-query-operators
        var usersQuery =
            from user in query
            join fnl in _dbContext.UserClaims
                on new { user.Id, ClaimType = JwtClaimTypes.GivenName }
                equals new { Id = fnl.UserId, fnl.ClaimType } into fnLeft
            from fn in fnLeft.DefaultIfEmpty()
            join lnl in _dbContext.UserClaims
                on new { user.Id, ClaimType = JwtClaimTypes.FamilyName }
                equals new { Id = lnl.UserId, lnl.ClaimType } into lnLeft
            from ln in lnLeft.DefaultIfEmpty()
            select new UserInfo {
                Id = user.Id,
                FirstName = fn.ClaimValue,
                LastName = ln.ClaimValue,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                UserName = user.UserName,
                CreateDate = user.CreateDate,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                TwoFactorEnabled = user.TwoFactorEnabled,
                Blocked = user.Blocked,
                PasswordExpirationPolicy = user.PasswordExpirationPolicy,
                IsAdmin = user.Admin,
                AccessFailedCount = user.AccessFailedCount,
                LastSignInDate = user.LastSignInDate,
                PasswordExpirationDate = user.PasswordExpirationDate
            };
        if (options?.Search?.Length > 2) {
            var userSearchFilterExpression = await IdentityDbContextOptions.UserSearchFilter(_dbContext, options.Search);
            usersQuery = usersQuery.Where(userSearchFilterExpression);
        }
        if (options?.Filter?.UserId?.Any() == true) {
            usersQuery = usersQuery.Where(x => options.Filter.UserId.Contains(x.Id));
        }
        return Ok(await usersQuery.ToResultSetAsync(options));
    }

    /// <summary>Gets a user by its unique id.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [CacheResourceFilter(Expiration = 1)]
    [HttpGet("{userId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetUser([FromRoute] string userId) {
        var foundUser = await (
            from user in _dbContext.Users.AsNoTracking()
            where user.Id == userId
            select new SingleUserInfo {
                Id = userId,
                CreateDate = user.CreateDate,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnabled ? (user.LockoutEnd > DateTimeOffset.UtcNow ? user.LockoutEnd : null) : null,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                UserName = user.UserName,
                Blocked = user.Blocked,
                PasswordExpirationPolicy = user.PasswordExpirationPolicy,
                IsAdmin = user.Admin,
                AccessFailedCount = user.AccessFailedCount,
                LastSignInDate = user.LastSignInDate,
                PasswordExpirationDate = user.PasswordExpirationDate,
                Claims = user.Claims.Select(claim => new ClaimInfo {
                    Id = claim.Id,
                    Type = claim.ClaimType,
                    Value = claim.ClaimValue
                })
                .ToList(),
                Roles = _dbContext.UserRoles.Where(role => role.UserId == userId).Join(
                    _dbContext.Roles,
                    userRole => userRole.RoleId,
                    role => role.Id,
                    (userRole, role) => role.Name
                )
                .ToList()
            }
        )
        .SingleOrDefaultAsync();
        if (foundUser is null) {
            return NotFound();
        }
        var userClaimIds = foundUser.Claims.Select(claim => claim.Type).ToList();
        if (userClaimIds.Any()) {
            var claimTypes = await _configurationDbContext.ClaimTypes.Where(claim => userClaimIds.Contains(claim.Name)).ToListAsync();
            foreach (var claim in foundUser.Claims) {
                var claimType = claimTypes.SingleOrDefault(x => x.Name == claim.Type);
                if (claimType != null) {
                    claim.DisplayName = claimType.DisplayName;
                }
            }
        }
        return Ok(foundUser);
    }

    /// <summary>Creates a new user.</summary>
    /// <param name="request">Contains info about the user to be created.</param>
    /// <response code="201">Created</response>
    /// <response code="400">Bad Request</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [HttpPost]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SingleUserInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) {
        var user = new User {
            CreateDate = DateTime.UtcNow,
            Email = request.Email,
            EmailConfirmed = request.EmailConfirmed ?? false,
            Id = $"{Guid.NewGuid()}",
            PasswordExpirationPolicy = request.PasswordExpirationPolicy,
            PhoneNumber = request.PhoneNumber,
            PhoneNumberConfirmed = request.PhoneNumberConfirmed ?? false,
            TwoFactorEnabled = request.TwoFactorEnabled ?? false,
            UserName = request.UserName
        };
        IdentityResult result = null;
        if (string.IsNullOrEmpty(request.Password)) {
            result = await _userManager.CreateAsync(user);
        } else {
            result = await _userManager.CreateAsync(user, request.Password, validatePassword: !request.BypassPasswordValidation.GetValueOrDefault());
        }
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (request.ChangePasswordAfterFirstSignIn.HasValue && request.ChangePasswordAfterFirstSignIn.Value == true) {
            await _userManager.SetPasswordExpiredAsync(user, true);
        }
        var claims = request?.Claims?.Count() > 0 ? request.Claims.Select(x => new Claim(x.Type, x.Value)).ToList() : new List<Claim>();
        if (!string.IsNullOrEmpty(request.FirstName)) {
            claims.Add(new Claim(JwtClaimTypes.GivenName, request.FirstName));
        }
        if (!string.IsNullOrEmpty(request.LastName)) {
            claims.Add(new Claim(JwtClaimTypes.FamilyName, request.LastName));
        }
        if (claims.Any()) {
            await _userManager.AddClaimsAsync(user, claims);
        }
        var response = SingleUserInfo.FromUser(user);
        return CreatedAtAction(nameof(GetUser), Name, new { userId = user.Id }, response);
    }

    /// <summary>Updates an existing user.</summary>
    /// <param name="userId">The id of the user to update.</param>
    /// <param name="request">Contains info about the user to update.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter]
    [HttpPut("{userId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserRequest request) {
        var user = await _dbContext.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        if (_userManager.EmailAsUserName && !request.BypassEmailAsUserNamePolicy && request.UserName != request.Email) {
            ModelState.AddModelError(nameof(request.UserName), "EmailAsUserName policy is applied to the identity system. Email and UserName properties should have the same value. User is not updated.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        user.UserName = request.UserName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.TwoFactorEnabled = request.TwoFactorEnabled;
        user.PasswordExpirationPolicy = request.PasswordExpirationPolicy;
        user.Admin = request.IsAdmin;
        user.EmailConfirmed = request.EmailConfirmed;
        user.PhoneNumberConfirmed = request.PhoneNumberConfirmed;
        foreach (var requiredClaim in request.Claims ?? Enumerable.Empty<BasicClaimInfo>()) {
            var claim = user.Claims.SingleOrDefault(x => x.ClaimType == requiredClaim.Type);
            if (claim != null) {
                claim.ClaimValue = requiredClaim.Value;
            } else {
                user.Claims.Add(new IdentityUserClaim<string> {
                    UserId = userId,
                    ClaimType = requiredClaim.Type,
                    ClaimValue = requiredClaim.Value
                });
            }
        }
        var identityResult = await _userManager.UpdateAsync(user, request.BypassEmailAsUserNamePolicy);
        if (!identityResult.Succeeded) {
            return BadRequest(identityResult.Errors.ToValidationProblemDetails());
        }
        var roles = await _dbContext.UserRoles.AsNoTracking().Where(x => x.UserId == userId).Join(
            _dbContext.Roles,
            userRole => userRole.RoleId,
            role => role.Id,
            (userRole, role) => role.Name
        )
        .ToListAsync();
        return Ok(new SingleUserInfo {
            Id = userId,
            CreateDate = user.CreateDate,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            PhoneNumber = user.PhoneNumber,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            UserName = user.UserName,
            Blocked = user.Blocked,
            Claims = user.Claims.Select(x => new ClaimInfo {
                Id = x.Id,
                Type = x.ClaimType,
                Value = x.ClaimValue
            })
            .ToList(),
            Roles = roles
        });
    }

    /// <summary>Resends the confirmation email for a given user.</summary>
    /// <response code="200">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [HttpPost("{userId}/email/confirmation")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> ResendConfirmationEmail([FromRoute] string userId) {
        var user = await _dbContext.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        if (await _userManager.IsEmailConfirmedAsync(user)) {
            ModelState.AddModelError(string.Empty, "User's email is already confirmed.");
            return BadRequest(new ValidationProblemDetails(ModelState));
        }
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _eventService.Publish(new UserEmailConfirmationResendEvent(user, token));
        return NoContent();
    }

    /// <summary>Permanently deletes a user.</summary>
    /// <param name="userId">The id of the user to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter]
    [HttpDelete("{userId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteUser([FromRoute] string userId) {
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        await _userManager.DeleteAsync(user);
        return NoContent();
    }

    /// <summary>Adds a new role to the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="roleId">The id of the role.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = ["{userId}"])]
    [HttpPost("{userId}/roles/{roleId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddUserRole([FromRoute] string userId, [FromRoute] string roleId) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) {
            return NotFound();
        }
        if (await _userManager.IsInRoleAsync(user, role.Name)) {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                { $"{nameof(roleId)}", new[] { $"User {user.Email} is already a member of role {role.Name}." } }
            }));
        }
        var result = await _userManager.AddToRoleAsync(user, role.Name);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (role.IsManagementRole()) {
            var clientId = User.FindFirst(JwtClaimTypes.ClientId);
            await _persistedGrantService.RemoveAllGrantsAsync(userId, clientId?.Value);
        }
        return NoContent();
    }

    /// <summary>Removes an existing role from the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="roleId">The id of the role.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpDelete("{userId}/roles/{roleId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteUserRole([FromRoute] string userId, [FromRoute] string roleId) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) {
            return NotFound();
        }
        if (!await _userManager.IsInRoleAsync(user, role.Name)) {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                { $"{nameof(roleId)}", new[] { $"User {user.Email} is not a member of role {role.Name}." } }
            }));
        }
        var result = await _userManager.RemoveFromRoleAsync(user, role.Name);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (role.IsManagementRole()) {
            await _persistedGrantService.RemoveAllGrantsAsync(userId);
        }
        return NoContent();
    }

    /// <summary>Gets a specified claim for a given user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="claimId">The id of the claim.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [CacheResourceFilter]
    [HttpGet("{userId}/claims/{claimId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(BasicClaimInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetUserClaim([FromRoute] string userId, [FromRoute] int claimId) {
        var claim = await _dbContext.UserClaims.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
        if (claim == null) {
            return NotFound();
        }
        return Ok(new BasicClaimInfo {
            Type = claim.ClaimType,
            Value = claim.ClaimValue
        });
    }

    /// <summary>Adds a claim for the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="request">The claim to add.</param>
    /// <response code="201">Created</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpPost("{userId}/claims")]
    [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClaimInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> AddUserClaim([FromRoute] string userId, [FromBody] CreateClaimRequest request) {
        var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        var claimToAdd = new IdentityUserClaim<string> {
            UserId = userId,
            ClaimType = request.Type,
            ClaimValue = request.Value
        };
        _dbContext.UserClaims.Add(claimToAdd);
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetUserClaim), Name, new { userId, claimId = claimToAdd.Id }, new ClaimInfo {
            Id = claimToAdd.Id,
            Type = claimToAdd.ClaimType,
            Value = claimToAdd.ClaimValue
        });
    }

    /// <summary>Updates an existing user claim.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="claimId">The id of the user claim.</param>
    /// <param name="request">Contains info about the user claim to update.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpPut("{userId}/claims/{claimId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClaimInfo))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UpdateUserClaim([FromRoute] string userId, [FromRoute] int claimId, [FromBody] UpdateUserClaimRequest request) {
        var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
        if (userClaim == null) {
            return NotFound();
        }
        userClaim.ClaimValue = request.ClaimValue;
        await _dbContext.SaveChangesAsync();
        return Ok(new ClaimInfo {
            Id = userClaim.Id,
            Type = userClaim.ClaimType,
            Value = request.ClaimValue
        });
    }

    /// <summary>Permanently deletes a specified claim from a user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="claimId">The id of the claim to delete.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpDelete("{userId}/claims/{claimId}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteUserClaim([FromRoute] string userId, [FromRoute] int claimId) {
        var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
        if (userClaim == null) {
            return NotFound();
        }
        _dbContext.Remove(userClaim);
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Gets a list of the applications the user has given consent to or currently has IdentityServer side tokens for.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <response code="200">OK</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [HttpGet("{userId}/applications")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserClientInfo>))]
    public async Task<IActionResult> GetUserApplications([FromRoute] string userId) {
        var userGrants = await _persistedGrantService.GetAllGrantsAsync(userId);
        var clients = new List<UserClientInfo>();
        foreach (var grant in userGrants) {
            var client = await _clientStore.FindClientByIdAsync(grant.ClientId);
            if (client != null) {
                clients.Add(new UserClientInfo {
                    ClientId = client.ClientId,
                    ClientName = client.ClientName,
                    ClientUri = client.ClientUri,
                    Description = client.Description,
                    LogoUri = client.LogoUri,
                    RequireConsent = client.RequireConsent,
                    AllowRememberConsent = client.AllowRememberConsent,
                    Enabled = client.Enabled,
                    CreatedAt = grant.CreationTime,
                    ExpiresAt = grant.Expiration,
                    Scopes = grant.Scopes
                });
            }
        }
        return Ok(clients.ToResultSet());
    }

    /// <summary>Gets a list of the devices of the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <response code="200">OK</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [HttpGet("{userId}/devices")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<DeviceInfo>))]
    public async Task<IActionResult> GetUserDevices([FromRoute] string userId) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var devices = await _userManager.GetDevicesAsync(user);
        var response = devices.Select(device => new DeviceInfo {
            ClientType = device.ClientType,
            Data = device.Data,
            DateCreated = device.DateCreated,
            DeviceId = device.DeviceId,
            IsPushNotificationsEnabled = device.IsPushNotificationsEnabled,
            IsTrusted = device.IsTrusted,
            LastSignInDate = device.LastSignInDate,
            Model = device.Model,
            Name = device.Name,
            OsVersion = device.OsVersion,
            Platform = device.Platform,
            SupportsFingerprintLogin = device.SupportsFingerprintLogin,
            SupportsPinLogin = device.SupportsPinLogin
        })
        .ToResultSet();
        return Ok(response);
    }

    /// <summary>Gets a list of the external login providers for the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <response code="200">OK</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [HttpGet("{userId}/external-logins")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserLoginProviderInfo>))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> GetUserExternalLogins([FromRoute] string userId) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var externalLogins = await _userManager.GetLoginsAsync(user);
        return Ok(externalLogins.Select(x => new UserLoginProviderInfo {
            Key = x.ProviderKey,
            Name = x.LoginProvider,
            DisplayName = !string.IsNullOrWhiteSpace(x.ProviderDisplayName) ? x.ProviderDisplayName : x.LoginProvider
        })
        .ToResultSet());
    }

    /// <summary>Gets a list of the external login providers for the specified user.</summary>
    /// <param name="userId">The id of the user.</param>
    /// <param name="provider">The provider to remove.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersReader)]
    [HttpDelete("{userId}/external-logins/{provider}")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteUserExternalLogin([FromRoute] string userId, [FromRoute] string provider) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var externalLogins = await _userManager.GetLoginsAsync(user);
        var externalLogin = externalLogins.SingleOrDefault(x => x.LoginProvider == provider);
        if (externalLogin == null) {
            return NotFound();
        }
        var result = await _userManager.RemoveLoginAsync(user, externalLogin.LoginProvider, externalLogin.ProviderKey);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Toggles user block state.</summary>
    /// <param name="userId">The id of the user to block.</param>
    /// <param name="request">Contains info about whether to block the user or not.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpPut("{userId}/block")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> SetUserBlock([FromRoute] string userId, [FromBody] SetUserBlockRequest request) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) {
            return NotFound();
        }
        var result = await _userManager.SetBlockedAsync(user, request.Blocked);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return NoContent();
    }

    /// <summary>Unlocks a user.</summary>
    /// <param name="userId">The id of the user to unlock.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpPut("{userId}/unlock")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> UnlockUser([FromRoute] string userId) {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) {
            return NotFound();
        }
        var result = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        result = await _userManager.ResetAccessFailedCountAsync(user);
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        return Ok();
    }

    /// <summary>Sets the password for a given user.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="request">Contains info about the user password to change.</param>
    /// <response code="204">No Content</response>
    /// <response code="400">Bad Request</response>
    /// <response code="404">Not Found</response>
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Policies.BeUsersWriter)]
    [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
    [HttpPut("{userId}/set-password")]
    [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
    public async Task<IActionResult> SetPassword([FromRoute] string userId, [FromBody] SetPasswordRequest request) {
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
        if (user == null) {
            return NotFound();
        }
        var result = await _userManager.ResetPasswordAsync(user, request.Password, validatePassword: !request.BypassPasswordValidation.GetValueOrDefault());
        if (!result.Succeeded) {
            return BadRequest(result.Errors.ToValidationProblemDetails());
        }
        if (request.ChangePasswordAfterFirstSignIn == true) {
            await _userManager.SetPasswordExpiredAsync(user, true);
        }
        return NoContent();
    }
}
