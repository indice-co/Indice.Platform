using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Server.Integration;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminAccessRulesHandlers
{
    /// <summary>Get Access rules.</summary>
    /// <param name="options"></param>
    /// <param name="filters">Filters to narrow down the results</param>
    /// <param name="accessRuleService"/>
    public static async Task<Ok<ResultSet<AccessRule>>> GetAccessRules([AsParameters] ListOptions options, [AsParameters] GetAccessRulesListFilter filters, IAccessRuleService accessRuleService) =>
        TypedResults.Ok(await accessRuleService.GetList(ListOptions.Create(options, filters)));

    /// <summary>Get Access rules for the specified case.</summary>
    /// <param name="caseId"></param>
    /// <param name="accessRuleService"></param>
    public static async Task<Ok<List<AccessRule>>> GetCaseAccessRules(Guid caseId, IAccessRuleService accessRuleService) =>
        TypedResults.Ok(await accessRuleService.GetListByCase(caseId));

    /// <summary>Add a new Access rule for admin Users.</summary>
    public static async Task<NoContent> CreateAccessRule(AddAccessRuleRequest request, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.Create(user.UserToActor(casesOptions.Value), request);
        return TypedResults.NoContent();
    }

    /// <summary>Add a list of new access rules for admin Users.</summary>
    public static async Task<NoContent> CreateAccessRulesBatch(List<AddAccessRuleRequest> request, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.BatchCreate(user.UserToActor(casesOptions.Value), request);
        return TypedResults.NoContent();
    }

    /// <summary>Update a specific Case Type.</summary>
    /// <param name="ruleId">Rule to be updated id</param>
    /// <param name="accessLevel">new access level</param>
    /// <param name="user"></param>
    /// <param name="casesOptions"></param>
    /// <param name="accessRuleService"></param>
    public static async Task<NoContent> UpdateAccessRule(Guid ruleId, int accessLevel, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.Update(user.UserToActor(casesOptions.Value), ruleId, accessLevel);
        return TypedResults.NoContent();
    }

    /// <summary>Delete a specific Access rule.</summary>
    public static async Task<NoContent> DeleteAccessRule(Guid ruleId, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.Delete(user.UserToActor(casesOptions.Value), ruleId);
        return TypedResults.NoContent();
    }

    /// <summary>Add a new Access rule for a specific case</summary>
    /// <param name="caseId">Case type Id</param>
    /// <param name="request">Rule grants</param>
    /// <param name="user"></param>
    /// <param name="casesOptions"></param>
    /// <param name="accessRuleService"></param>
    /// <returns></returns>
    public static async Task<NoContent> CreateCaseAccessRules(Guid caseId, AddCaseAccessRuleRequest request, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.CreateForCase(user.UserToActor(casesOptions.Value), caseId, request);
        return TypedResults.NoContent();
    }

    public static async Task<NoContent> UpdateCaseAccessRulesBatch(Guid caseId, List<AddCaseAccessRuleRequest> request, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        await accessRuleService.BatchCreateForCase(user.UserToActor(casesOptions.Value), caseId, request);
        return TypedResults.NoContent();
    }
    /// <summary>Replace user to the specified case with another</summary>
    /// <param name="caseId">Case type Id</param>
    /// <param name="request">The users for the replace</param>
    /// <param name="user"></param>
    /// <param name="casesOptions"></param>
    /// <param name="accessRuleService"></param>
    /// <returns></returns>
    public static async Task<Results<NoContent, NotFound>> ReplaceAccessRulesUser(Guid caseId, ReplaceCaseAccessRuleUserRequest request, ClaimsPrincipal user, IOptions<CasesOptions> casesOptions, IAccessRuleService accessRuleService) {
        var succeeded = await accessRuleService.ReplaceUser(user.UserToActor(casesOptions.Value), caseId, request.ExistingUserId, request.ReplacementUserId);
        if (!succeeded) {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
