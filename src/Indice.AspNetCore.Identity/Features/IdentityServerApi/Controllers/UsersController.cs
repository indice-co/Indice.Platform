using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Indice.AspNetCore.Filters;
using Indice.AspNetCore.Identity.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing applications users.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/users")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.SubScopes.Users)]
    [ProblemDetailsExceptionFilter]
    internal class UsersController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly IPersistedGrantService _persistedGrantService;
        private readonly IClientStore _clientStore;
        private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
        private readonly IEventService _eventService;
        private readonly GeneralSettings _generalSettings;
        private readonly IStringLocalizer<UsersController> _localizer;
        private readonly ExtendedConfigurationDbContext _configurationDbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Users";

        /// <summary>
        /// Creates an instance of <see cref="UsersController"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
        /// <param name="dbContext">Class for the Entity Framework database context used for identity.</param>
        /// <param name="persistedGrantService">Implements persisted grant logic.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
        /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
        /// <param name="generalSettings">General settings for an ASP.NET Core application.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="UsersController"/>.</param>
        /// <param name="configurationDbContext">Extended DbContext for the IdentityServer configuration data.</param>
        public UsersController(
            ExtendedUserManager<User> userManager, 
            RoleManager<Role> roleManager, 
            ExtendedIdentityDbContext<User, Role> dbContext, 
            IPersistedGrantService persistedGrantService,
            IClientStore clientStore, 
            IdentityServerApiEndpointsOptions apiEndpointsOptions, 
            IEventService eventService, 
            IOptions<GeneralSettings> generalSettings, 
            IStringLocalizer<UsersController> localizer,
            ExtendedConfigurationDbContext configurationDbContext
        ) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _configurationDbContext = configurationDbContext ?? throw new ArgumentNullException(nameof(configurationDbContext));
        }

        /// <summary>
        /// Returns a list of <see cref="UserInfo"/> objects containing the total number of users in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserInfo>))]
        [CacheResourceFilter(Expiration = 1)]
        public async Task<IActionResult> GetUsers([FromQuery] ListOptions<UserListFilter> options) {
            var query = _dbContext.Users.AsNoTracking();
            if (options?.Filter != null) {
                var filter = options.Filter;
                query = query.Where(x => filter.Claim == null || x.Claims.Any(x => x.ClaimType == filter.Claim.Type && x.ClaimValue == filter.Claim.Value));
            }
            var usersQuery = 
                from user in query
                join fnl in _dbContext.UserClaims on user.Id equals fnl.UserId into fnLeft
                from fn in fnLeft.DefaultIfEmpty()
                join lnl in _dbContext.UserClaims on user.Id equals lnl.UserId into lnLeft
                from ln in lnLeft.DefaultIfEmpty()
                where (fn == null || fn.ClaimType == JwtClaimTypes.GivenName) && (ln == null || ln.ClaimType == JwtClaimTypes.FamilyName)
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
                }
            ;
            if (options?.Search?.Length > 2) {
                var searchTerm = options.Search.ToLower();
                var idsFromClaims = await _dbContext.UserClaims.Where(x => (x.ClaimType == JwtClaimTypes.GivenName || x.ClaimType == JwtClaimTypes.FamilyName) && EF.Functions.Like(x.ClaimValue, $"%{searchTerm}%")).Select(x => x.UserId).ToArrayAsync();
                usersQuery = usersQuery.Where(x => EF.Functions.Like(x.Email, $"%{searchTerm}%")
                 || EF.Functions.Like(x.PhoneNumber, $"%{searchTerm}%")
                 || EF.Functions.Like(x.UserName, $"%{searchTerm}%")
                 || EF.Functions.Like(x.Email, $"%{searchTerm}%")
                 || searchTerm == x.Id.ToLower()
                 || idsFromClaims.Contains(x.Id));
            }
            return Ok(await usersQuery.ToResultSetAsync(options));
        }

        /// <summary>
        /// Gets a user by its unique id.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{userId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(Expiration = 60)]
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
                    LockoutEnd = user.LockoutEnd,
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
            if (foundUser == null) {
                return NotFound();
            }
            var claimTypes = await _configurationDbContext.ClaimTypes.ToListAsync();
            foreach (var claim in foundUser.Claims) {
                var claimType = claimTypes.SingleOrDefault(x => x.Name == claim.Type);
                if (claimType != null) {
                    claim.DisplayName = claimType.DisplayName;
                }
            }
            return Ok(foundUser);
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="request">Contains info about the user to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SingleUserInfo))]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request) {
            var user = new User {
                Id = $"{Guid.NewGuid()}",
                UserName = request.UserName,
                Email = request.Email,
                CreateDate = DateTime.UtcNow,
                PhoneNumber = request.PhoneNumber,
                PasswordExpirationPolicy = request.PasswordExpirationPolicy
            };
            IdentityResult result = null;
            if (string.IsNullOrEmpty(request.Password)) {
                result = await _userManager.CreateAsync(user);
            } else {
                result = await _userManager.CreateAsync(user, request.Password);
            }
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
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
            var response = new SingleUserInfo {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PasswordExpirationPolicy = user.PasswordExpirationPolicy,
                IsAdmin = user.Admin,
                TwoFactorEnabled = user.TwoFactorEnabled,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                Claims = user.Claims?.Select(x => new ClaimInfo {
                    Id = x.Id,
                    Type = x.ClaimType,
                    Value = x.ClaimValue
                })
                .ToList()
            };
            await _eventService.Raise(new UserCreatedEvent(response));
            return CreatedAtAction(nameof(GetUser), Name, new { userId = user.Id }, response);
        }

        /// <summary>
        /// Updates an existing user.
        /// </summary>
        /// <param name="userId">The id of the user to update.</param>
        /// <param name="request">Contains info about the user to update.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(SingleUserInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> UpdateUser([FromRoute] string userId, [FromBody] UpdateUserRequest request) {
            var user = await _dbContext.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            user.UserName = request.UserName;
            user.PhoneNumber = request.PhoneNumber;
            user.Email = request.Email;
            user.TwoFactorEnabled = request.TwoFactorEnabled;
            user.PasswordExpirationPolicy = request.PasswordExpirationPolicy;
            user.Admin = request.IsAdmin;
            user.EmailConfirmed = request.EmailConfirmed;
            user.PhoneNumberConfirmed = request.PhoneNumberConfirmed;
            foreach (var requiredClaim in request.Claims) {
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
            await _userManager.UpdateAsync(user);
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

        /// <summary>
        /// Permanently deletes a user.
        /// </summary>
        /// <param name="userId">The id of the user to delete.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{userId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
        public async Task<IActionResult> DeleteUser([FromRoute] string userId) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            await _userManager.DeleteAsync(user);
            return NoContent();
        }

        /// <summary>
        /// Adds a new role to the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="roleId">The id of the role.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{userId}/roles/{roleId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
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
            await _userManager.AddToRoleAsync(user, role.Name);
            return NoContent();
        }

        /// <summary>
        /// Removes an existing role from the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="roleId">The id of the role.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{userId}/roles/{roleId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
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
            await _userManager.RemoveFromRoleAsync(user, role.Name);
            return NoContent();
        }

        /// <summary>
        /// Gets a specified claim for a given user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the claim.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(BasicClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter]
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

        /// <summary>
        /// Adds a claim for the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="request">The claim to add.</param>
        /// <response code="201">Created</response>
        /// <response code="404">Not Found</response>
        [HttpPost("{userId}/claims")]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
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

        /// <summary>
        /// Updates an existing user claim.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the user claim.</param>
        /// <param name="request">Contains info about the user claim to update.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClaimInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
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

        /// <summary>
        /// Permanently deletes a specified claim from a user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="claimId">The id of the claim to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{userId}/claims/{claimId}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> DeleteUserClaim([FromRoute] string userId, [FromRoute] int claimId) {
            var userClaim = await _dbContext.UserClaims.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == claimId);
            if (userClaim == null) {
                return NotFound();
            }
            _dbContext.Remove(userClaim);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Gets a list of the applications the user has given consent to or currently has IdentityServer side tokens for.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpGet("{userId}/applications")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserClientInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
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

        /// <summary>
        /// Toggles user block state.
        /// </summary>
        /// <param name="userId">The id of the user to block.</param>
        /// <param name="request">Contains info about whether to block the user or not.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/set-block")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> SetUserBlock([FromRoute] string userId, [FromBody] SetUserBlockRequest request) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            user.Blocked = request.Blocked;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            // When blocking a user we need to make sure we also revoke all of his tokens.
            await _persistedGrantService.RemoveAllGrantsAsync(userId);
            return NoContent();
        }

        /// <summary>
        /// Unlocks a user.
        /// </summary>
        /// <param name="userId">The id of the user to unlock.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/unlock")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> UnlockUser([FromRoute] string userId) {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            result = await _userManager.ResetAccessFailedCountAsync(user);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            return Ok();
        }

        /// <summary>
        /// Sets the password for a given user.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="204">No Content</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/set-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status204NoContent, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> SetPassword([FromRoute] string userId, [FromBody] SetPasswordRequest request) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            var hasPassword = await _userManager.HasPasswordAsync(user);
            IdentityResult result;
            if (hasPassword) {
                result = await _userManager.RemovePasswordAsync(user);
                if (!result.Succeeded) {
                    return BadRequest(result.Errors.AsValidationProblemDetails());
                }
            }
            result = await _userManager.AddPasswordAsync(user, request.Password);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            if (request.ChangePasswordAfterFirstSignIn.HasValue && request.ChangePasswordAfterFirstSignIn.Value == true) {
                await _userManager.SetPasswordExpiredAsync(user, true);
            }
            result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.AsValidationProblemDetails());
            }
            return NoContent();
        }
    }
}
