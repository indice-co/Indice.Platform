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
using Indice.AspNetCore.Identity.Services;
using Indice.Configuration;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
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
    internal class UserController : ControllerBase
    {
        private readonly ExtendedUserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        private readonly IPersistedGrantService _persistedGrantService;
        private readonly IClientStore _clientStore;
        private readonly IdentityServerApiEndpointsOptions _apiEndpointsOptions;
        private readonly IEventService _eventService;
        private readonly GeneralSettings _generalSettings;
        private readonly IStringLocalizer<UserController> _localizer;
        private readonly EmailVerificationOptions _userEmailVerificationOptions;
        private readonly IEmailService _emailService;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "User";

        /// <summary>
        /// Creates an instance of <see cref="UserController"/>.
        /// </summary>
        /// <param name="userManager">Provides the APIs for managing user in a persistence store.</param>
        /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
        /// <param name="dbContext">Class for the Entity Framework database context used for identity.</param>
        /// <param name="persistedGrantService">Implements persisted grant logic.</param>
        /// <param name="clientStore">Retrieval of client configuration.</param>
        /// <param name="apiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
        /// <param name="eventService">Models the event mechanism used to raise events inside the IdentityServer API.</param>
        /// <param name="generalSettings">General settings for an ASP.NET Core application.</param>
        /// <param name="localizer">Represents an <see cref="IStringLocalizer"/> that provides strings for <see cref="UserController"/>.</param>
        /// <param name="userEmailVerificationOptions">Options for the email sent to user for verification.</param>
        /// <param name="emailService">A service responsible for sending emails.</param>
        public UserController(ExtendedUserManager<User> userManager, RoleManager<Role> roleManager, ExtendedIdentityDbContext<User, Role> dbContext, IPersistedGrantService persistedGrantService, IClientStore clientStore,
            IdentityServerApiEndpointsOptions apiEndpointsOptions, IEventService eventService, IOptions<GeneralSettings> generalSettings, IStringLocalizer<UserController> localizer,
            EmailVerificationOptions userEmailVerificationOptions = null, IEmailService emailService = null) {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _persistedGrantService = persistedGrantService ?? throw new ArgumentNullException(nameof(persistedGrantService));
            _clientStore = clientStore ?? throw new ArgumentNullException(nameof(clientStore));
            _apiEndpointsOptions = apiEndpointsOptions ?? throw new ArgumentNullException(nameof(apiEndpointsOptions));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _generalSettings = generalSettings?.Value ?? throw new ArgumentNullException(nameof(generalSettings));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            _userEmailVerificationOptions = userEmailVerificationOptions;
            _emailService = emailService;
        }

        public string UserId => User.FindFirstValue(JwtClaimTypes.Subject);
        public string UserName => User.FindFirstValue(JwtClaimTypes.Name);

        /// <summary>
        /// Returns a list of <see cref="UserInfo"/> objects containing the total number of users in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<UserInfo>))]
        public async Task<ActionResult<ResultSet<UserInfo>>> GetUsers([FromQuery]ListOptions<UserListFilter> options) {
            var query = _userManager.Users.AsNoTracking();
            if (!string.IsNullOrEmpty(options?.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Email.ToLower().Contains(searchTerm)
                                      || x.PhoneNumber.Contains(searchTerm)
                                      || x.UserName.ToLower().Contains(searchTerm)
                                      || x.Claims.Any(x => x.ClaimValue.ToLower().Contains(searchTerm))
                                      || searchTerm.Contains(x.Id.ToLower()));
            }
            if (options?.Filter != null) {
                var filter = options.Filter;
                query = query.Where(x => filter.Claim == null || x.Claims.Any(x => x.ClaimType == filter.Claim.Type && x.ClaimValue == filter.Claim.Value));
            }
            var users = await query.Select(x => new UserInfo {
                Id = x.Id,
                FirstName = x.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.GivenName).ClaimValue,
                LastName = x.Claims.FirstOrDefault(x => x.ClaimType == JwtClaimTypes.FamilyName).ClaimValue,
                Email = x.Email,
                EmailConfirmed = x.EmailConfirmed,
                PhoneNumber = x.PhoneNumber,
                PhoneNumberConfirmed = x.PhoneNumberConfirmed,
                UserName = x.UserName,
                CreateDate = x.CreateDate,
                LockoutEnabled = x.LockoutEnabled,
                LockoutEnd = x.LockoutEnd,
                TwoFactorEnabled = x.TwoFactorEnabled,
                Blocked = x.Blocked,
                PasswordExpirationPolicy = x.PasswordExpirationPolicy,
                IsAdmin = x.Admin,
                AccessFailedCount = x.AccessFailedCount,
                Claims = x.Claims.Select(x => new ClaimInfo {
                    Id = x.Id,
                    Type = x.ClaimType,
                    Value = x.ClaimValue
                })
            })
            .ToResultSetAsync(options);
            return Ok(users);
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
        [CacheResourceFilter]
        public async Task<ActionResult<SingleUserInfo>> GetUser([FromRoute]string userId) {
            var user = await _dbContext.Users
                                       .AsNoTracking()
                                       .Where(x => x.Id == userId)
                                       .Select(x => new SingleUserInfo {
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
                                           Blocked = x.Blocked,
                                           PasswordExpirationPolicy = x.PasswordExpirationPolicy,
                                           IsAdmin = x.Admin,
                                           AccessFailedCount = x.AccessFailedCount,
                                           Claims = x.Claims.Select(x => new ClaimInfo {
                                               Id = x.Id,
                                               Type = x.ClaimType,
                                               Value = x.ClaimValue
                                           })
                                          .ToList(),
                                           Roles = _dbContext.UserRoles.Where(x => x.UserId == userId).Join(
                                               _dbContext.Roles,
                                               userRole => userRole.RoleId,
                                               role => role.Id,
                                               (userRole, role) => role.Name
                                           )
                                           .ToList()
                                       })
                                       .SingleOrDefaultAsync();
            if (user == null) {
                return NotFound();
            }
            return Ok(user);
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="request">Contains info about the user to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(SingleUserInfo))]
        public async Task<ActionResult<SingleUserInfo>> CreateUser([FromBody]CreateUserRequest request) {
            var user = new User {
                Id = $"{Guid.NewGuid()}",
                UserName = request.UserName,
                Email = request.Email,
                CreateDate = DateTime.UtcNow,
                PhoneNumber = request.PhoneNumber,
                PasswordExpirationPolicy = request.PasswordExpirationPolicy
            };
            user.PasswordExpirationDate = user.CalculatePasswordExpirationDate();
            IdentityResult result = null;
            if (string.IsNullOrEmpty(request.Password)) {
                result = await _userManager.CreateAsync(user);
            } else {
                result = await _userManager.CreateAsync(user, request.Password);
            }
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
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
            if (request.SendConfirmationEmail) {
                await SendEmailConfirmation(user);
            }
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
        public async Task<ActionResult<SingleUserInfo>> UpdateUser([FromRoute]string userId, [FromBody]UpdateUserRequest request) {
            var user = await _dbContext.Users.Include(x => x.Claims).SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            user.UserName = request.UserName;
            user.PhoneNumber = request.PhoneNumber;
            user.Email = request.Email;
            user.LockoutEnabled = request.LockoutEnabled;
            user.TwoFactorEnabled = request.TwoFactorEnabled;
            user.LockoutEnd = request.LockoutEnd;
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
        public async Task<IActionResult> DeleteUser([FromRoute]string userId) {
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
        public async Task<IActionResult> DeleteUserRole([FromRoute]string userId, [FromRoute]string roleId) {
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
        public async Task<ActionResult<BasicClaimInfo>> GetUserClaim([FromRoute]string userId, [FromRoute]int claimId) {
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
        public async Task<ActionResult<ClaimInfo>> AddUserClaim([FromRoute]string userId, [FromBody]CreateClaimRequest request) {
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
        public async Task<ActionResult<ClaimInfo>> UpdateUserClaim([FromRoute]string userId, [FromRoute]int claimId, [FromBody]UpdateUserClaimRequest request) {
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
        public async Task<IActionResult> DeleteUserClaim([FromRoute]string userId, [FromRoute]int claimId) {
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
            return Ok(clients.ToResultSet());
        }

        /// <summary>
        /// Blocks a user permanently.
        /// </summary>
        /// <param name="userId">The id of the user to block.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/block")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> BlockUser([FromRoute]string userId) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            if (user.Blocked) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { "blocked", new[] { $"User {user.Email} is already locked." } }
                }));
            }
            user.Blocked = true;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        /// <summary>
        /// Unblocks a user.
        /// </summary>
        /// <param name="userId">The id of the user to unblock.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/unblock")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> UnblockUser([FromRoute]string userId) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            if (!user.Blocked) {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> {
                    { "blocked", new[] { $"User {user.Email} is already not locked." } }
                }));
            }
            user.Blocked = false;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        /// <summary>
        /// Unlocks a user.
        /// </summary>
        /// <param name="userId">The id of the user to unlock.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/unlock")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> UnlockUser([FromRoute]string userId) {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) {
                return NotFound();
            }
            // Setting an end date tin the past immediately unlocks the user.
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

        /// <summary>
        /// Sets the password for a given user.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <param name="request">Contains info about the user password to change.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{userId}/set-password")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(void))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [CacheResourceFilter(DependentPaths = new string[] { "{userId}" })]
        public async Task<IActionResult> SetPassword([FromRoute]string userId, [FromBody]SetPasswordRequest request) {
            var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId);
            if (user == null) {
                return NotFound();
            }
            var hasPassword = await _userManager.HasPasswordAsync(user);
            IdentityResult result;
            if (hasPassword) {
                result = await _userManager.RemovePasswordAsync(user);
                if (!result.Succeeded) {
                    return BadRequest(result.Errors.ToValidationProblemDetails());
                }
            }
            result = await _userManager.AddPasswordAsync(user, request.Password);
            if (!result.Succeeded) {
                return BadRequest(result.Errors.ToValidationProblemDetails());
            }
            if (request.ChangePasswordAfterFirstSignIn.HasValue && request.ChangePasswordAfterFirstSignIn.Value == true) {
                user.PasswordExpirationPolicy = PasswordExpirationPolicy.NextLogin;
                await _userManager.UpdateAsync(user);
            }
            return Ok();
        }

        private async Task SendEmailConfirmation(User user) {
            if (_userEmailVerificationOptions == null) {
                return;
            }
            if (_emailService == null) {
                throw new Exception($"No concrete implementation of {nameof(IEmailService)} is registered. Check {nameof(ServiceCollectionExtensions.AddEmailService)}, {nameof(ServiceCollectionExtensions.AddEmailServiceSmtpRazor)} or " +
                    $"{nameof(ServiceCollectionExtensions.AddEmailServiceSparkpost)} extensions on {nameof(IServiceCollection)} or provide your own implementation.");
            }
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{_generalSettings.Host}{Url.Action(nameof(MyAccountController.ConfirmEmail), MyAccountController.Name, new { userId = user.Id, code })}";
            var recipient = user.Email;
            var subject = _userEmailVerificationOptions.Subject;
            var body = _userEmailVerificationOptions.Body.Replace("{callbackUrl}", callbackUrl);
            var data = new User {
                UserName = User.FindDisplayName() ?? user.UserName
            };
            await _emailService.SendAsync<User>(message => message.To(recipient).WithSubject(subject).WithBody(body).WithData(data));
        }
    }
}
