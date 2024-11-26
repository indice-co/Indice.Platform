using System.Net.Mime;
using Indice.Features.Cases.Extensions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Json.Patch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Case Data administrative actions.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage/cases")]
public class AdminCaseDataController : ControllerBase
{
    private readonly IAdminCaseService _adminCaseService;

    /// <summary>AdminCaseDataController Constructor.</summary>
    public AdminCaseDataController(IAdminCaseService adminCaseService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    /// <summary>
    /// Patches the Case.Data object with an object passed in the body.
    /// If Data is invalid due to schema validation failure, no change will happen and an error 500 will be returned
    /// Recursively merges two JsonNodes by ensuring that the structure of the `original` node is
    /// updated defensively preventing overwrites of incompatible JsonNode types.
    /// 1. If the `toMerge` node contains multiple nested types, each one of them should exist as-is in the `original` node.
    /// 2. If the `toMerge` node contains nested types that the `original` node only partially matches (subset),
    ///   the remaining nested types are added to the corresponding location in the `original` node.
    /// 3. When we encounter a JsonArray we add/replace from THIS POINT ON the nested element at the end of the array,
    /// and NOT replace any existing - nested or not - items.
    /// 
    /// <b>JsonIgnoreCondition.WhenWritingNull must NOT be set in the nswag client serializer if you want to remove a property</b>
    /// https://indice.visualstudio.com/Platform/_wiki/wikis/Platform.wiki/1613/Patch-Case-Data-API">Documentation
    /// For handling nested arrays, moving elements, checking if data exists at specified locations use JsonPatch API
    /// </summary>
    /// <param name="caseId"></param>
    /// <param name="patch">
    /// <code>
    /// _casesApiClient.PatchAdminCaseDataAsync(caseId, null, new { t1 = "test", t2 = (object)null! }
    /// </code>
    /// <b>If the path is found "add" works as add or replace.</b>
    /// <b>This will NOT create non existing paths, be sure to specify the full object as value on a JsonPointer that exists.</b>
    /// </param>
    [HttpPatch("{caseId:guid}/data")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PatchAdminCaseData([FromRoute] Guid caseId, [FromBody] object patch) {
        await _adminCaseService.PatchCaseData(User, caseId,  patch);
        return NoContent();
    }
    
    /// <summary>
    /// Update the Case Data for the specific caseId according to https://datatracker.ietf.org/doc/html/rfc6902#appendix-A
    /// Example Usage:
    /// </summary>
    /// <param name="request">
    /// <code>
    /// _casesApiClient.JsonPatchAdminCaseDataAsync(caseId, null, new PatchJsonPathRequest[] {
    ///   Operations = new PatchOperation[] {
    ///     new() { Op = OperationType.Add, Path = "/t1", Value = "test" },
    ///     new() { Op = OperationType.Remove, Path = "/t2" }
    /// }
    /// </code>
    /// <b>If the path is found "add" works as add or replace.</b>
    /// <b>This will NOT create non existing paths, be sure to specify the full object as value on a JsonPointer that exists.</b>
    /// </param>
    [HttpPatch("{caseId:guid}/data-json")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> JsonPatchAdminCaseData([FromRoute] Guid caseId, [FromBody] List<PatchJsonPathRequest> request) {
        var operations = request.Select(op => op.ToPatchOperation()).ToList();
        await _adminCaseService.PatchCaseData(User, caseId,  new JsonPatch(operations));
        return NoContent();
    }
}