using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing application roles.
    /// </summary>
    [GenericControllerNameConvention]
    [Route("api/roles")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    internal class RoleController<TRole> : ControllerBase where TRole : Role, new()
    {
        private readonly RoleManager<TRole> _roleManager;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "Role";

        /// <summary>
        /// Creates an instance of <see cref="RoleController{TRole}"/>.
        /// </summary>
        /// <param name="roleManager">Provides the APIs for managing roles in a persistence store.</param>
        public RoleController(RoleManager<TRole> roleManager) {
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        /// <summary>
        /// Returns a list of <see cref="RoleInfo"/> objects containing the total number of roles in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpGet]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ResultSet<RoleInfo>))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ResultSet<RoleInfo>>> GetRoles([FromQuery]ListOptions options) {
            var query = _roleManager.Roles.AsNoTracking();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
            }
            var roles = await query.Select(x => new RoleInfo {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description
            })
            .ToResultSetAsync(options);
            return Ok(roles);
        }

        /// <summary>
        /// Gets a role by it's unique id.
        /// </summary>
        /// <param name="id">The identifier of the role.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(RoleInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleInfo>> GetRole([FromRoute]string id) {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }
            return Ok(new RoleInfo {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            });
        }

        /// <summary>
        /// Creates a new role.
        /// </summary>
        /// <param name="request">Contains info about the role to be created.</param>
        /// <response code="201">Created</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPost]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(RoleInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        public async Task<ActionResult<RoleInfo>> CreateRole([FromBody]CreateRoleRequest request) {
            var role = new TRole {
                Id = $"{Guid.NewGuid()}",
                Name = request.Name,
                Description = request.Description
            };
            var result = await _roleManager.CreateAsync(role);
            return CreatedAtAction(nameof(GetRole), Name, new { id = role.Id }, new RoleInfo {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            });
        }

        /// <summary>
        /// Updates an existing role.
        /// </summary>
        /// <param name="id">The id of the role to update.</param>
        /// <param name="request">Contains info about the role to update.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpPut("{id}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(RoleInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<RoleInfo>> UpdateRole([FromRoute]string id, [FromBody]UpdateRoleRequest request) {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }
            role.Description = request.Description;
            await _roleManager.UpdateAsync(role);
            return Ok(new RoleInfo {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description
            });
        }

        /// <summary>
        /// Permanently deletes a role.
        /// </summary>
        /// <param name="id">The id of the role to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, type: typeof(ValidationProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status403Forbidden, type: typeof(ProblemDetails))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteRole([FromRoute]string id) {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) {
                return NotFound();
            }
            await _roleManager.DeleteAsync(role);
            return Ok();
        }
    }
}
