using System.Net.Mime;
using System.Security.AccessControl;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Controllers;

/// <summary>Cases Access rules from the administrative perspective.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = ApiGroups.CasesApiGroupNamePlaceholder)]
[Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Route($"{ApiPrefixes.CasesApiTemplatePrefixPlaceholder}/manage")]
internal class AdminAccessRulesController : ControllerBase {
    private readonly IAccessRuleService _accessRuleService;

    public AdminAccessRulesController(IAccessRuleService accessRuleService) {
        _accessRuleService = accessRuleService;
    }

    /// <summary>Get Access rules.</summary>
    /// <param name="filters">Filters to narrow down the results</param>
    [HttpGet("access-rules")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<AccessRule>))]
    public async Task<IActionResult> GetAccessRules([FromQuery] ListOptions<GetAccessRulesListFilter> filters) {
        var AccessRuless = await _accessRuleService.Get(filters);
        return Ok(AccessRuless);
    }

    /// <summary>Get Access rules for the specified case.</summary>
    /// <param name="caseId"></param>
    [HttpGet("cases/{caseId:Guid}/access-rules")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResultSet<AccessRule>))]
    public async Task<IActionResult> GetAccessRulesForCase(Guid caseId) {
        var AccessRuless = await _accessRuleService.GetCaseAccessRules(caseId);
        return Ok(AccessRuless);
    }


    /// <summary>Add a new Access rule for admin Users.</summary>
    [HttpPost("access-rules/admin")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateAccessRuleAdmin([FromBody] AddAccessRuleRequest request) {
        await _accessRuleService.AdminCreate(User, request);
        return NoContent();
    }

    /// <summary>Add a new Access rule for admin Users.</summary>
    [HttpPost("access-rules/admin/batch")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CreateBatchAccessRulesAdmin([FromBody] List<AddAccessRuleRequest> request) {
        await _accessRuleService.AdminBatch(User, request);
        return NoContent();
    }


    /// <summary>Add a new Access rule for a specific case</summary>
    /// <param name="caseId">Case type Id</param>
    /// <param name="request">Rule grants</param>
    /// <returns></returns>
    [HttpPost("access-rules/case/{caseId:guid}")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesManager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<IActionResult> CreateAccessRules([FromRoute] Guid caseId, [FromBody] AddCaseAccessRuleRequest request) {
        await _accessRuleService.Create(User, caseId, request);
        return NoContent();
    }

    [HttpPut("access-rules/case/{caseId:guid}/batch")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme, Policy = CasesApiConstants.Policies.BeCasesAdministrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAccessRules([FromRoute] Guid caseId, [FromBody] List<AddCaseAccessRuleRequest> request) {
        await _accessRuleService.Batch(User, caseId, request);
        return NoContent();
    }

    /// <summary>Update a specific Case Type.</summary>
    /// <param name="ruleId">Rule to be updated id</param>
    /// <param name="accessLevel">new access level</param>
    [HttpPut("access-rules/{ruleId:guid}/{accessLevel:int}")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AccessRule))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<IActionResult> UpdateAccessRules([FromRoute] Guid ruleId, [FromRoute] int accessLevel) {
        var AccessRulesDetails = await _accessRuleService.Update(User, ruleId, accessLevel);
        return Ok(AccessRulesDetails);
    }

    /// <summary>Delete a specific Access rule.</summary>
    [HttpDelete("access-rules/{ruleId:guid}")]
    [Authorize(AuthenticationSchemes = CasesApiConstants.AuthenticationScheme)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<IActionResult> DeleteAccessRules(Guid ruleId) {
        await _accessRuleService.Delete(User, ruleId);
        return NoContent();
    }
}