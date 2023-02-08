using System.Net.Mime;
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
        /// Get cases grouped by status
        /// </summary>
        /// <returns></returns>
        [HttpGet("grouped-by-status")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GroupByReportResult>))]
        public async Task<IActionResult> GetCasesGroupedByStatus() {
            var result = await _adminReportService.GetCasesGroupedByStatus();
            return Ok(result);
        }

        /// <summary>
        /// Get cases grouped by casetype
        /// </summary>
        /// <returns></returns>
        [HttpGet("grouped-by-casetype")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GroupByReportResult>))]
        public async Task<IActionResult> GetCasesGroupedByCaseType() {
            var result = await _adminReportService.GetCasesGroupedByCaseType();
            return Ok(result);
        }

        /// <summary>
        /// Get cases grouped by GroupId
        /// </summary>
        /// <returns></returns>
        [HttpGet("grouped-by-groupid")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<GroupByReportResult>))]
        public async Task<IActionResult> GetCasesGroupedByGroupId() {
            var result = await _adminReportService.GetCasesGroupedByGroupId();
            return Ok(result);
        }

    }
}
