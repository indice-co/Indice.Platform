using System.Net.Mime;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Manage queries for Back-office users.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = CasesApiConstants.Scope)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route("[casesApiPrefix]/manage/queries")]
internal class AdminQueriesController : ControllerBase
{
    private readonly IQueryService _queryService;

    public AdminQueriesController(IQueryService queryService) {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    }

    /// <summary>Get saved queries.</summary>
    /// <response code="200">OK</response>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Query>))]
    public async Task<IActionResult> GetQueries() {
        var queries = await _queryService.GetQueries(User);
        return Ok(queries);
    }

    /// <summary>Save a new query.</summary>
    /// <param name="request"></param>
    /// <response code="204">No Content</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SaveQuery([FromBody] SaveQueryRequest request) {
        await _queryService.SaveQuery(User, request);
        return NoContent();
    }

    /// <summary>Delete a query.</summary>
    /// <param name="queryId">The id of the query.</param>
    /// <response code="204">No Content</response>
    /// <response code="404">Not Found</response>
    [HttpDelete("{queryId:guid}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> DeleteQuery([FromRoute] Guid queryId) {
        await _queryService.DeleteQuery(User, queryId);
        return NoContent();
    }
}