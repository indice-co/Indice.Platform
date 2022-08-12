using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage check point types.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/checkpoint-types")]
    internal class AdminCheckpointTypesController : ControllerBase
    {
        private readonly ICheckpointTypeService _checkpointTypeService;

        public AdminCheckpointTypesController(ICheckpointTypeService checkpointTypeService) {
            _checkpointTypeService = checkpointTypeService ?? throw new ArgumentNullException(nameof(checkpointTypeService));
        }

        /// <summary>
        /// Get Checkpoint types
        /// </summary>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<string>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetDistinctCheckpointNames() {
            var checkpointTypes = await _checkpointTypeService.GetDistinctCheckpointNames(User);
            return Ok(checkpointTypes);
        }
    }
}