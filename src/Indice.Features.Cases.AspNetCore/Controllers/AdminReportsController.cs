using System.Net.Mime;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers
{
    /// <summary>
    /// Manage cases reports and everything related to cases for back-office users.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Route("[casesApiPrefix]/manage/reports")]
    internal class AdminReportsController : ControllerBase
    {
        private readonly IAdminReportService _adminReportService;

        public AdminReportsController(IAdminReportService adminReportService) {
            _adminReportService = adminReportService ?? throw new ArgumentNullException(nameof(adminReportService));
        }

        /// <summary>
        /// Get case report
        /// </summary>
        /// <param name="reportTag"></param>
        /// <returns></returns>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GroupByReportResult>))]
        public async Task<IActionResult> GetCaseReport(ReportTag reportTag) {
            var result = await _adminReportService.GenerateReport(reportTag);
            return Ok(result);
        }
    }
}
