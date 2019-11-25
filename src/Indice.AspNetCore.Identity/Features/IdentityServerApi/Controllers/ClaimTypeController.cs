using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.AspNetCore.Identity.Models;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Contains operations for managing application claim types.
    /// </summary>
    /// <response code="400">Bad Request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal Server Error</response>
    [Route("api/claim-types")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "identity")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: 400, type: typeof(ValidationProblemDetails))]
    [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
    [Authorize(AuthenticationSchemes = IdentityServerApi.AuthenticationScheme, Policy = IdentityServerApi.Admin)]
    [CacheResourceFilter]
    [ProblemDetailsExceptionFilter]
    internal class ClaimTypeController : ControllerBase
    {
        private readonly ExtendedIdentityDbContext<User, Role> _dbContext;
        /// <summary>
        /// The name of the controller.
        /// </summary>
        public const string Name = "ClaimType";

        /// <summary>
        /// Creates an instance of <see cref="ClaimTypeController"/>.
        /// </summary>
        /// <param name="dbContext"><see cref="DbContext"/> for the Identity Framework.</param>
        public ClaimTypeController(ExtendedIdentityDbContext<User, Role> dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Returns a list of <see cref="ClaimTypeInfo"/> objects containing the total number of claim types in the database and the data filtered according to the provided <see cref="ListOptions"/>.
        /// </summary>
        /// <param name="options">List params used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        /// <response code="200">OK</response>
        [HttpGet]
        [ProducesResponseType(statusCode: 200, type: typeof(ResultSet<ClaimTypeInfo>))]
        [NoCache]
        public async Task<ActionResult<ResultSet<ClaimTypeInfo>>> GetClaimTypes([FromQuery]ListOptions<ClaimTypesListFilter> options) {
            var query = _dbContext.ClaimTypes.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(options.Search)) {
                var searchTerm = options.Search.ToLower();
                query = query.Where(x => x.Name.ToLower().Contains(searchTerm) || x.Description.Contains(searchTerm));
            }
            if (options.Filter?.Required.HasValue == true) {
                query = query.Where(x => x.Required == options.Filter.Required.Value);
            }
            var claimTypes = await query.Select(x => new ClaimTypeInfo {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Rule = x.Rule,
                ValueType = x.ValueType,
                Required = x.Required,
                Reserved = x.Reserved,
                UserEditable = x.UserEditable
            })
            .ToResultSetAsync(options);
            return Ok(claimTypes);
        }

        /// <summary>
        /// Gets a claim type by it's unique id.
        /// </summary>
        /// <param name="id">The identifier of the claim type.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClaimTypeInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        [HttpGet("{id}")]
        public async Task<ActionResult<ClaimTypeInfo>> GetClaimType([FromRoute]string id) {
            var claimType = await _dbContext.ClaimTypes.AsNoTracking().Select(x => new ClaimTypeInfo {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Description = x.Description,
                Rule = x.Rule,
                ValueType = x.ValueType,
                Required = x.Required,
                Reserved = x.Reserved,
                UserEditable = x.UserEditable
            })
            .SingleOrDefaultAsync(x => x.Id == id);
            if (claimType == null) {
                return NotFound();
            }
            return Ok(claimType);
        }

        /// <summary>
        /// Creates a new claim type.
        /// </summary>
        /// <param name="request">Contains info about the claim to be created.</param>
        /// <response code="201">Created</response>
        [HttpPost]
        [ServiceFilter(type: typeof(CreateClaimTypeRequestValidationFilter))]
        [ProducesResponseType(statusCode: StatusCodes.Status201Created, type: typeof(ClaimTypeInfo))]
        public async Task<ActionResult<ClaimTypeInfo>> CreateClaimType([FromBody]CreateClaimTypeRequest request) {
            var claimType = new ClaimType {
                Id = $"{Guid.NewGuid()}",
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Rule = request.Rule,
                ValueType = request.ValueType,
                Required = request.Required,
                Reserved = false,
                UserEditable = request.UserEditable
            };
            _dbContext.ClaimTypes.Add(claimType);
            await _dbContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetClaimType), Name, new { id = claimType.Id }, new ClaimTypeInfo {
                Id = claimType.Id,
                Name = claimType.Name,
                DisplayName = claimType.DisplayName,
                Description = claimType.Description,
                Rule = claimType.Rule,
                ValueType = claimType.ValueType,
                Required = claimType.Required,
                Reserved = claimType.Reserved,
                UserEditable = claimType.UserEditable
            });
        }

        /// <summary>
        /// Updates an existing claim type.
        /// </summary>
        /// <param name="id">The id of the claim to update.</param>
        /// <param name="request">Contains info about the claim to update.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK, type: typeof(ClaimTypeInfo))]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<ActionResult<ClaimTypeInfo>> UpdateClaimType([FromRoute]string id, [FromBody]UpdateClaimTypeRequest request) {
            var claimType = await _dbContext.ClaimTypes.SingleOrDefaultAsync(x => x.Id == id);
            if (claimType == null) {
                return NotFound();
            }
            // Modify claim type properties according to request model.
            claimType.DisplayName = request.DisplayName;
            claimType.Description = request.Description;
            claimType.Rule = request.Rule;
            claimType.ValueType = request.ValueType;
            claimType.Required = request.Required;
            claimType.UserEditable = request.UserEditable;
            // Commit changes to database.
            await _dbContext.SaveChangesAsync();
            // Send the response.
            return Ok(new ClaimTypeInfo {
                Id = claimType.Id,
                Name = claimType.Name,
                DisplayName = claimType.DisplayName,
                Description = claimType.Description,
                Rule = claimType.Rule,
                ValueType = claimType.ValueType,
                Required = claimType.Required,
                Reserved = claimType.Reserved,
                UserEditable = claimType.UserEditable
            });
        }

        /// <summary>
        /// Permanently deletes an existing claim type.
        /// </summary>
        /// <param name="id">The id of the claim to delete.</param>
        /// <response code="200">OK</response>
        /// <response code="404">Not Found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, type: typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteClaimType([FromRoute]string id) {
            var claimType = await _dbContext.ClaimTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            if (claimType == null) {
                return NotFound();
            }
            _dbContext.ClaimTypes.Remove(claimType);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
