using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

internal class AdminAccessRulesHandler
{
    /// <summary>Get Access rules.</summary>
    /// <param name="options"></param>
    /// <param name="filters">Filters to narrow down the results</param>
    /// <param name="accessRuleService"/>
    public static async Task<Ok<ResultSet<AccessRule>>> GetAccessRules([AsParameters] ListOptions options, [AsParameters] GetAccessRulesListFilter filters, IAccessRuleService accessRuleService) =>
        TypedResults.Ok(await accessRuleService.Get(ListOptions.Create(options, filters)));

    /// <summary>Get Access rules for the specified case.</summary>
    /// <param name="caseId"></param>
    /// <param name="accessRuleService"></param>
    public static async Task<Ok<List<AccessRule>>> GetAccessRulesForCase(Guid caseId, IAccessRuleService accessRuleService) =>
        TypedResults.Ok(await accessRuleService.GetCaseAccessRules(caseId));

    /// <summary>Add a new Access rule for admin Users.</summary>
    public static async Task<NoContent> CreateAccessRuleAdmin(AddAccessRuleRequest request, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.AdminCreate(user, request);
        return TypedResults.NoContent();
    }

    /// <summary>Add a list of new access rules for admin Users.</summary>
    public static async Task<NoContent> CreateBatchAccessRulesAdmin(List<AddAccessRuleRequest> request, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.AdminBatch(user, request);
        return TypedResults.NoContent();
    }

    /// <summary>Add a new Access rule for a specific case</summary>
    /// <param name="caseId">Case type Id</param>
    /// <param name="request">Rule grants</param>
    /// <param name="user"></param>
    /// <param name="accessRuleService"></param>
    /// <returns></returns>
    public static async Task<NoContent> CreateAccessRules(Guid caseId,AddCaseAccessRuleRequest request, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.Create(user, caseId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateBatchAccessRules(Guid caseId, List<AddCaseAccessRuleRequest> request, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.Batch(user, caseId, request);
        return TypedResults.NoContent();
    }

    /// <summary>Update a specific Case Type.</summary>
    /// <param name="ruleId">Rule to be updated id</param>
    /// <param name="accessLevel">new access level</param>
    /// <param name="user"></param>
    /// <param name="accessRuleService"></param>
    public static async Task<NoContent> UpdateAccessRule(Guid ruleId, int accessLevel, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.Update(user, ruleId, accessLevel);
        return TypedResults.NoContent();
    }

    /// <summary>Delete a specific Access rule.</summary>
    public static async Task<NoContent> DeleteAccessRule(Guid ruleId, ClaimsPrincipal user, IAccessRuleService accessRuleService)
    {
        await accessRuleService.Delete(user, ruleId);
        return TypedResults.NoContent();
    }
}
