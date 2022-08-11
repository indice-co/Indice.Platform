using System;
using System.Net.Mime;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Case types from the customer's perspective.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesUser)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/my/case-types")]
    internal class MyCaseTypesController : ControllerBase
    {
        private readonly IMyCaseService _myCaseService;

        public MyCaseTypesController(IMyCaseService myCaseService) {
            _myCaseService = myCaseService ?? throw new ArgumentNullException(nameof(myCaseService));
        }

        /// <summary>
        /// Gets a case type by its code.
        /// </summary>
        /// <param name="caseTypeCode">The case type code.</param>
        [ProducesResponseType(200, Type = typeof(CaseTypePartial))]
        [ProducesResponseType(400, Type = typeof(ValidationProblemDetails))]
        [ProducesResponseType(401, Type = typeof(ProblemDetails))]
        [ProducesResponseType(403, Type = typeof(ProblemDetails))]
        [Produces(MediaTypeNames.Application.Json)]
        [HttpGet("{caseTypeCode}")]
        public async Task<ActionResult> GetCaseType(string caseTypeCode) {
            var results = await _myCaseService.GetCaseType(caseTypeCode);
            return Ok(results);
        }
    }
}