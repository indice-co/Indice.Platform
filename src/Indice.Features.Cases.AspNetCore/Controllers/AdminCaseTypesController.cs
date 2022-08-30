using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage case types.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/case-types")]
    internal class AdminCaseTypesController : ControllerBase
    {
        private readonly ICaseTypeService _caseTypeService;

        public AdminCaseTypesController(ICaseTypeService caseTypeService) {
            _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
        }

        /// <summary>
        /// Get case types.
        /// </summary>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<CaseTypePartial>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCaseTypes() {
            var caseTypes = await _caseTypeService.Get(User);
            return Ok(caseTypes);
        }

        /// <summary>
        /// Get a specific Case Type by Id.
        /// </summary>
        /// <param name="caseTypeId">The case type Id. </param>
        [HttpGet("{caseTypeId:guid}")]
        [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeAdministrator)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CaseTypeDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetCaseTypeById(Guid caseTypeId) {
            var caseTypeDetails = await _caseTypeService.GetCaseTypeDetailsById(caseTypeId);
            return Ok(caseTypeDetails);
        }

        /// <summary>
        /// Create new case type.
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeAdministrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> CreateCaseType([FromBody] CaseTypeRequest request) {
            await _caseTypeService.Create(request);
            return NoContent();
        }

        /// <summary>
        /// Update a specific Case Type.
        /// </summary>
        /// <param name="request">The new case type model.</param>
        [HttpPut("{caseTypeId:guid}")]
        [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeAdministrator)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CaseTypeDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> UpdateCaseType([FromBody] CaseTypeRequest request) {
            var caseTypeDetails = await _caseTypeService.Update(request);
            return Ok(caseTypeDetails);
        }

        /// <summary>
        /// Delete a specific Case Type.
        /// </summary>
        [HttpDelete("{caseTypeId:guid}")]
        [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeAdministrator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteCaseType(Guid caseTypeId) {
            await _caseTypeService.Delete(caseTypeId);
            return NoContent();
        }
    }
}