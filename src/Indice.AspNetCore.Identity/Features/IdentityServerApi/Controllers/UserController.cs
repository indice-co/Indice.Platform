using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Extensions;
using Indice.AspNetCore.Identity.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing application's users.
    /// </summary>
    [GenericControllerNameConvention]
    [Route("api/users")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.SubScopes.Users)]
    internal class UserController<TUser, TRole> : ControllerBase
        where TUser : User, new()
        where TRole : Role, new()
    {
        private readonly UserManager<TUser> _userManager;
        private readonly RoleManager<TRole> _roleManager;
        private readonly IDistributedCache _cache;
        private readonly ExtendedIdentityDbContext<TUser, TRole> _dbContext;
        private readonly IPersistedGrantService _persistedGrantService;
        private readonly IClientStore _clientStore;
        private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
        private readonly IEventService _eventService;

        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "User";

        /// <summary>
        /// Creates an instance of <see cref="UserController{TUser, TRole}"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
        /// <param name="cache">Represents a distributed cache of serialized values.</param>
        /// <param name="dbContext">Class for the Entity Framework database context used for identity.</param>
        /// <param name="persistedGrantService">Implements persisted grant logic.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
        /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
        public UserController(UserManager<TUser> userManager, RoleManager<TRole> roleManager, IDistributedCache cache, ExtendedIdentityDbContext<TUser, TRole> dbContext, IPersistedGrantService persistedGrantService, IClientStore clientStore,
            IdentityServerApiEndpointsOptions apiEndpointsOptions, IEventService eventService) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        /// <summary>
        /// Returns a list of <see cref="UserInfo"/> objects containing the total number of users in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<UserInfo>>> GetUsers([FromQuery]ListOptions options) {
            var query = _userManager.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Email.ToLower().Contains(searchTerm)
                                      || x.PhoneNumber.Contains(searchTerm)
                                      || x.UserName.ToLower().Contains(searchTerm));
            }
            // Creating a new 'UserInfo' object at this point will result in evaluating the query in-memory.
            var users = await query.Select(x => new UserInfo {
                Id = x.Id,
                FirstName = x.Claims.FirstOrDefault(claim => claim.ClaimType == JwtClaimTypes.GivenName).ClaimValue,
                LastName = x.Claims.FirstOrDefault(claim => claim.ClaimType == JwtClaimTypes.FamilyName).ClaimValue,
                Email = x.Email,
                EmailConfirmed = x.EmailConfirmed,
                PhoneNumber = x.PhoneNumber,
                PhoneNumberConfirmed = x.PhoneNumberConfirmed,
                UserName = x.UserName,
                CreateDate = x.CreateDate,
                LockoutEnabled = x.LockoutEnabled,
                LockoutEnd = x.LockoutEnd,
                TwoFactorEnabled = x.TwoFactorEnabled
            })
            .ToResultSetAsync(options);
            return Ok(users);
        }

        /// <summary>
        /// Gets a user by it's unique id.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("{userId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<SingleUserInfo>> GetUser([FromRoute]string userId) {
            async Task<SingleUserInfo> GetUserAsync() {
                // Load user with his claims and roles from the database.
                var query = _dbContext.Users.AsNoTracking();
                var foundUser = await query.Where(x => x.Id == userId).Select(x => new SingleUserInfo {
                    Id = userId,
                    CreateDate = x.CreateDate,
                    Email = x.Email,
                    EmailConfirmed = x.EmailConfirmed,
                    LockoutEnabled = x.LockoutEnabled,
                    LockoutEnd = x.LockoutEnd,
                    PhoneNumber = x.PhoneNumber,
                    PhoneNumberConfirmed = x.PhoneNumberConfirmed,
                    TwoFactorEnabled = x.TwoFactorEnabled,
                    UserName = x.UserName,
                    Claims = x.Claims.Select(userClaim => new UserClaimInfo {
                        Id = userClaim.Id,
                        ClaimType = userClaim.ClaimType,
                        ClaimValue = userClaim.ClaimValue
                    })
                    .ToList(),
                    Roles = _dbContext.UserRoles.Where(userRole => userRole.UserId == userId).Join(
                        _dbContext.Roles,
                        userRole => userRole.RoleId,
                        role => role.Id,
                        (userRole, role) => role.Name
                    )
                    .ToList()
                })
                .SingleOrDefaultAsync();
                if (foundUser == null) {
                    return null;
                }
                return foundUser;
            }
            // Retrieve the user by either the cache or the database.
            var user = await _cache.TryGetOrSetAsync(CacheKeys.User(userId), GetUserAsync, TimeSpan.FromDays(7));
            if (user == null) {
                return NotFound();
            }
            // Return 200 status code containing the user information.
            return Ok(user);
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="request">Contains info about the user to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SingleUserInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<SingleUserInfo>> CreateUser([FromBody]CreateUserRequest request) {
            var user = new TUser {
                Id = $"{Guid.NewGuid()}",
                UserName = request.UserName,
                Email = request.Email,
                CreateDate = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) {
                return BadRequest(new ValidationProblemDetails(result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description })));
            }
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(request.FirstName)) {
                claims.Add(new Claim(JwtClaimTypes.GivenName, request.FirstName));
            }
            if (!string.IsNullOrEmpty(request.LastName)) {
                claims.Add(new Claim(JwtClaimTypes.FamilyName, request.LastName));
            }
            if (claims.Any()) {
                await _userManager.AddClaimsAsync(user, claims);
            }
            var response = new SingleUserInfo {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };
            if (_apiEndpointsOptions.RaiseEvents) {
                await _eventService.Raise(new UserCreatedEvent(response));
            }
            return CreatedAtAction(nameof(GetUser), Name, new { userId = user.Id }, response);
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="userId">The id of the user to update.</param>
        /// <param name="request">Contains info about the user to update.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut("{userId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<SingleUserInfo>> UpdateUser([FromRoute]string userId, [FromBody]UpdateUserRequest request) {
            // Load user with his claims from the database.
            var foundUser = await _dbContext.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Id == userId);
            if (foundUser == null) {
                return NotFound();
            }
            // Modify user properties according to request model.
            foundUser.UserName = request.UserName;
            foundUser.PhoneNumber = request.PhoneNumber;
            foundUser.Email = request.Email;
            foundUser.LockoutEnabled = request.LockoutEnabled;
            foundUser.TwoFactorEnabled = request.TwoFactorEnabled;
            foundUser.LockoutEnd = request.LockoutEnd;
            // Modify user required claims.
            foreach (var requiredClaim in request.Claims) {
                var claim = foundUser.Claims.SingleOrDefault(x => x.ClaimType == requiredClaim.ClaimType);
                if (claim != null) {
                    claim.ClaimValue = requiredClaim.ClaimValue;
                } else {
                    foundUser.Claims.Add(new IdentityUserClaim<string> {
                        UserId = userId,
                        ClaimType = requiredClaim.ClaimType,
                        ClaimValue = requiredClaim.ClaimValue
                    });
                }
            }
            // Update user and his claims.
            await _userManager.UpdateAsync(foundUser);
            // Retrieve user roles from database to prepare the response.
            var roles = await _dbContext.UserRoles.AsNoTracking().Where(x => x.UserId == userId).Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => role.Name
            )
            .ToListAsync();
            // Remove user entry from the cache, since the user is updated.
            await _cache.RemoveAsync(CacheKeys.User(userId));
            // Return 200 status code containing the updated user information.
            return Ok(new SingleUserInfo {
                Id = userId,
                CreateDate = foundUser.CreateDate,
                Email = foundUser.Email,
                EmailConfirmed = foundUser.EmailConfirmed,
                LockoutEnabled = foundUser.LockoutEnabled,
                LockoutEnd = foundUser.LockoutEnd,
                PhoneNumber = foundUser.PhoneNumber,
                PhoneNumberConfirmed = foundUser.PhoneNumberConfirmed,
                TwoFactorEnabled = foundUser.TwoFactorEnabled,
                UserName = foundUser.UserName,
                Claims = foundUser.Claims.Select(x => new UserClaimInfo {
                    Id = x.Id,
                    ClaimType = x.ClaimType,
                    ClaimValue = x.ClaimValue
                })
                .ToList(),
                Roles = roles
            });
        }

        /// <summary>
        /// Adds a new role to the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="roleId">The id of the role.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("{userId}/roles/{roleId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<IActionResult> AddUserRole([FromRoute]string userId, [FromRoute]string roleId) {
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
                    { $"{nameof(roleId)}", new[] { $"User '{user.Email}' is already a member of role '{role.Name}'." } }
                }));
            }
            await _userManager.AddToRoleAsync(user, role.Name);
            await _cache.RemoveAsync(CacheKeys.User(userId));
            return Ok();
        }

        /// <summary>
        /// Removes an existing role from the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="roleId">The id of the role.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("{userId}/roles/{roleId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> RemoveUserRole([FromRoute]string userId, [FromRoute]string roleId) {
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
                    { $"{nameof(roleId)}", new[] { $"User '{user.Email}' is not a member of role '{role.Name}'." } }
                }));
            }
            await _userManager.RemoveFromRoleAsync(user, role.Name);
            await _cache.RemoveAsync(CacheKeys.User(userId));
            return Ok();
        }

        /// <summary>
        /// Gets a specified claim for a given user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the claim.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(BasicUserClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<BasicUserClaimInfo>> GetUserClaim([FromRoute]string userId, [FromRoute]int claimId) {
            var claim = await _dbContext.UserClaims.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
            if (claim == null) {
                return NotFound();
            }
            return Ok(new BasicUserClaimInfo {
                ClaimType = claim.ClaimType,
                ClaimValue = claim.ClaimValue
            });
        }

        /// <summary>
        /// Adds or updates a claim for the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="request">The claim to add or update.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost("{userId}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(UserClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<UserClaimInfo>> AddUserClaim([FromRoute]string userId, [FromBody]CreateUserClaimRequest request) {
            var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            var claimToAdd = new IdentityUserClaim<string> {
                UserId = userId,
                ClaimType = request.ClaimType,
                ClaimValue = request.ClaimValue
            };
            _dbContext.UserClaims.Add(claimToAdd);
            await _dbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.User(userId));
            return CreatedAtAction(nameof(GetUserClaim), Name, new { userId, claimId = claimToAdd.Id }, new UserClaimInfo {
                Id = claimToAdd.Id,
                ClaimType = claimToAdd.ClaimType,
                ClaimValue = claimToAdd.ClaimValue
            });
        }

        /// <summary>
        /// Updates an existing user claim.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the user claim.</param>
        /// <param name="request">Contains info about the user claim to update.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(UserClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<UserClaimInfo>> UpdateUserClaim([FromRoute]string userId, [FromRoute]int claimId, [FromBody]UpdateUserClaimRequest request) {
            var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
            if (userClaim == null) {
                return NotFound();
            }
            userClaim.ClaimValue = request.ClaimValue;
            await _dbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.User(userId));
            return Ok(new UserClaimInfo {
                Id = userClaim.Id,
                ClaimType = userClaim.ClaimType,
                ClaimValue = request.ClaimValue
            });
        }

        /// <summary>
        /// Permanently deletes a specified claim from a user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the claim to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteUserClaim([FromRoute]string userId, [FromRoute]int claimId) {
            var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
            if (userClaim == null) {
                return NotFound();
            }
            _dbContext.Remove(userClaim);
            await _dbContext.SaveChangesAsync();
            await _cache.RemoveAsync(CacheKeys.User(userId));
            return Ok();
        }

        /// <summary>
        /// Gets a list of the applications the user has given consent to or currently has IdentityServer side tokens for.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet("{userId}/applications")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserClientInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<UserClientInfo>>> GetUserApplications([FromRoute]string userId) {
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
            return Ok(new ResultSet<UserClientInfo>(clients, clients.Count));
        }
    }
}
